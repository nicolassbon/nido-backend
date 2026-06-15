using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Recetas;
using Nido.Application.Common.Security;
using Nido.Application.Recetas;
using System.Text;
using System.Text.Json;
using System.Threading;

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
    // 🔥 NUEVO ENDPOINT: CONEXIÓN DINÁMICA CON MICROSERVICIO DE IA (LISTAS)
    // =====================================================================
    [HttpPost("ia-recomendar")]
public async Task<IActionResult> RecomendarPorIa(
    [FromBody] RecomendarIaRequest request,
    [FromServices] ICurrentUserContext currentUser,
    CancellationToken ct) // Paréntesis corregido acá
{
    if (string.IsNullOrWhiteSpace(request.Mensaje))
        return BadRequest("El mensaje no puede estar vacío.");

    // 1. Armamos el JSON para Python
    var payload = new { mensaje = request.Mensaje };
    var jsonPayload = JsonSerializer.Serialize(payload);
    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    var nombresSugeridos = new List<string>();

    try
    {
        // 2. Le pegamos al contenedor de Flask usando la red de Docker
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync("http://host.docker.internal:5000/api/ia/recomendar", httpContent, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseString);
            
            // 🌟 PARSEAMOS LA LISTA: Buscamos la propiedad "recetas" que mandó Python
            if (doc.RootElement.TryGetProperty("recetas", out var recetasElement) && recetasElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var elemento in recetasElement.EnumerateArray())
                {
                    var nombre = elemento.GetString();
                    if (!string.IsNullOrWhiteSpace(nombre))
                    {
                        nombresSugeridos.Add(nombre);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[IA-BACKEND] Error de conexión: {ex.Message}");
        return StatusCode(500, "Error al conectar con el motor de IA.");
    }

    // Si la IA no encontró nada o la lista vino vacía
    if (!nombresSugeridos.Any())
        return NotFound("La IA no pudo encontrar recetas que coincidan con tu búsqueda.");

    // 3. Traemos todas las recetas reales del hogar desde Postgres
    var todasLasRecetas = await _getRecetasHandler.Handle(currentUser.HogarId, ct);

    // 4. MATCHO MULTIPLE BLINDADO: Filtramos todas las recetas que coincidan con la lista de la IA
// =====================================================================
        // 4. FILTRADO MULTIPLE CON NORMALIZACIÓN (Paso 4 Completado)
        // =====================================================================
        
        // Función interna local para limpiar strings en caliente
        string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            
            var limpio = texto.ToLower().Trim().Replace("\n", "").Replace("\r", "");
            limpio = limpio.Replace("á", "a")
                           .Replace("é", "e")
                           .Replace("í", "i")
                           .Replace("ó", "o")
                           .Replace("ú", "u");
            return limpio;
        }

        // Filtramos comparando los textos normalizados de ambos lados
        var recetasMatcheadas = todasLasRecetas.Where(r =>
            nombresSugeridos.Any(ns => 
                NormalizarTexto(r.Nombre).Contains(NormalizarTexto(ns)) || 
                NormalizarTexto(ns).Contains(NormalizarTexto(r.Nombre))
            )
        ).ToList();

        // 🌟 AUDITORÍA EN CONSOLA DOCKER
        Console.WriteLine("=============================================================");
        Console.WriteLine($"[🔥 .NET AUDIT] Nombres que mandó la IA: {string.Join(", ", nombresSugeridos)}");
        Console.WriteLine($"[🔥 .NET AUDIT] Recetas en BD: {todasLasRecetas.Count} | Matchearon con éxito: {recetasMatcheadas.Count}");
        Console.WriteLine("=============================================================");

        // Si no hubo coincidencias después de limpiar, saltamos con 404
        if (!recetasMatcheadas.Any())
        {
            return NotFound("La IA sugirió opciones, pero ninguna coincidió en el formateo de la base de datos.");
        }

    // 5. Devolvemos la LISTA COMPLETA mapeada al Frontend
    return Ok(recetasMatcheadas.Select(ToResponse));
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