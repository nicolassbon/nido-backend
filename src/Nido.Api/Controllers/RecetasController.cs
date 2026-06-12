using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Recetas;
using Nido.Application.Common.Security;
using Nido.Application.Recetas;
using System.Text;
using System.Text.Json;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recetas")]
public sealed class RecetasController : ControllerBase
{
    private readonly GetRecetasHandler _getRecetasHandler;
    private readonly GetRecetaByIdHandler _getRecetaByIdHandler;
    private readonly CocinarRecetaHandler _cocinarRecetaHandler;
    private readonly IHttpClientFactory _httpClientFactory; // <-- El motor para hablar con Python

    public RecetasController(
        GetRecetasHandler getRecetasHandler,
        GetRecetaByIdHandler getRecetaByIdHandler,
        CocinarRecetaHandler cocinarRecetaHandler,
        IHttpClientFactory httpClientFactory) // <-- Inyectamos acá
    {
        _getRecetasHandler = getRecetasHandler;
        _getRecetaByIdHandler = getRecetaByIdHandler;
        _cocinarRecetaHandler = cocinarRecetaHandler;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetasHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetaByIdHandler.Handle(
            new GetRecetaByIdCommand(id, currentUser.HogarId), ct);

        if (result is null)
            return NotFound();

        return Ok(ToResponseFromById(result));
    }

    [HttpPost("{id}/cocinar")]
    public async Task<IActionResult> Cocinar(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _cocinarRecetaHandler.Handle(
            new CocinarRecetaCommand(id, currentUser.HogarId, currentUser.UsuarioId), ct);

        if (result is null)
            return NotFound();

        return Ok(new CocinarRecetaResponse(result.RecetaId, result.VecesCocinada));
    }

    // =====================================================================
    // 🔥 NUEVO ENDPOINT: CONEXIÓN CON EL MICROSERVICIO DE IA (PYTHON)
    // =====================================================================
    [HttpPost("ia-recomendar")]
    public async Task<IActionResult> RecomendarPorIa(
        [FromBody] RecomendarIaRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Mensaje))
            return BadRequest("El mensaje no puede estar vacío.");

        // 1. Preparamos el auto para ir al peaje: Armamos el JSON para Python
        var payload = new { mensaje = request.Mensaje };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        string nombreRecetaSugerida = "NONE";

        try
        {
            // 2. Le pegamos al peaje de Flask usando el HttpClient de .NET
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync("http://localhost:5000/api/ia/recomendar", httpContent, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseString);
                nombreRecetaSugerida = doc.RootElement.GetProperty("receta").GetString() ?? "NONE";
            }
        }
        catch (Exception ex)
        {
            // Si Flask está apagado, logueamos el error y dejamos que fluya el "NONE"
            Console.WriteLine($"[IA-BACKEND] Error de conexión: {ex.Message}");
        }

        // Si la IA no entendió o el peaje falló, devolvemos un NotFound decoroso
        if (nombreRecetaSugerida == "NONE")
            return NotFound("La IA no pudo encontrar una receta que coincida con tu búsqueda.");

        // 3. ¡EL TRUCO MASTER! Aprovechamos tus Handlers de C# para buscar el plato real
        // Traemos todas las recetas del hogar actual
        var todasLasRecetas = await _getRecetasHandler.Handle(currentUser.HogarId, ct);

        // Buscamos cuál de tus recetas de la base de datos coincide con el nombre que escupió el Mock
        var recetaMatcheada = todasLasRecetas.FirstOrDefault(r => 
            r.Nombre.Equals(nombreRecetaSugerida, StringComparison.OrdinalIgnoreCase));

        if (recetaMatcheada is null)
            return NotFound($"La IA sugirió '{nombreRecetaSugerida}', pero no se encontró en tu base de datos.");

        // 4. Devolvemos la receta completa en el formato exacto que espera tu Frontend
        return Ok(ToResponse(recetaMatcheada));
    }

    private static RecetaResponse ToResponse(RecetaResult receta)
    {
        return new RecetaResponse(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            receta.ImagenUrl,
            receta.Calorias,
            receta.Proteinas,
            receta.Carbohidratos,
            receta.Grasas,
            receta.Ingredientes.Select(ingrediente => new RecetaIngredienteResponse(
                ingrediente.Id,
                ingrediente.ProductoId,
                ingrediente.Nombre,
                ingrediente.ProductoNombre,
                ingrediente.Cantidad,
                ingrediente.Unidad,
                ingrediente.EnStock,
                ingrediente.Alergenos)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada);
    }

    private static RecetaResponse ToResponseFromById(GetRecetaByIdResult receta)
    {
        return new RecetaResponse(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            receta.ImagenUrl,
            receta.Calorias,
            receta.Proteinas,
            receta.Carbohidratos,
            receta.Grasas,
            receta.Ingredientes.Select(ingrediente => new RecetaIngredienteResponse(
                ingrediente.Id,
                ingrediente.ProductoId,
                ingrediente.Nombre,
                ingrediente.ProductoNombre,
                ingrediente.Cantidad,
                ingrediente.Unidad,
                ingrediente.EnStock,
                ingrediente.Alergenos)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada);
    }
}