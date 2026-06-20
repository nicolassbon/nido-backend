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
    // 🔥 ENDPOINT MODIFICADO: CONEXIÓN COMBINADA POR QUERY STRING (GET)
    // =====================================================================
    // 1. Declaramos una estructura simple para manejar los macros adentro de este controlador
public class MacrosIaDto
{
    public double Calorias { get; set; }
    public double Proteinas { get; set; }
    public double Carbohidratos { get; set; }
    public double Grasas { get; set; }
}

[HttpGet("ia/recomendar")] 
public async Task<IActionResult> RecomendarPorIa(
    [FromQuery] string? busqueda,
    [FromQuery] string? objetivo,
    [FromServices] ICurrentUserContext currentUser,
    CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(objetivo))
        return BadRequest("Debe ingresar un texto de búsqueda o seleccionar un objetivo nutricional.");

    var payload = new 
    { 
        mensaje = busqueda ?? "", 
        objetivo_nutricional = objetivo ?? "" 
    };
    
    var jsonPayload = JsonSerializer.Serialize(payload);
    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    var nombresSugeridos = new List<string>();

    try
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync("http://localhost:5000/api/ia/recomendar", httpContent, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseString);
            
            if (doc.RootElement.TryGetProperty("recetas", out var recetasElement) && recetasElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var elemento in recetasElement.EnumerateArray())
                {
                    if (elemento.TryGetProperty("nombre", out var nombreElem))
                    {
                        var nombre = nombreElem.GetString();
                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            // 1. Python ahora SOLO manda nombres reales. Los guardamos para buscar en Postgres.
                            nombresSugeridos.Add(nombre);
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[IA-BACKEND] Error de conexión con Flask: {ex.Message}");
        return StatusCode(500, "Error al conectar con el motor de IA.");
    }

    if (!nombresSugeridos.Any())
        return NotFound("La IA no pudo encontrar recetas que coincidan con tu búsqueda y objetivos.");

    // 2. Traemos todas las recetas reales del hogar desde Postgres (con sus valores nutricionales reales)
    var todasLasRecetas = await _getRecetasHandler.Handle(currentUser.HogarId, ct);

    // 3. FILTRADO CON NORMALIZACIÓN
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

    var recetasMatcheadas = todasLasRecetas.Where(r =>
        nombresSugeridos.Any(ns => 
            NormalizarTexto(r.Nombre).Contains(NormalizarTexto(ns)) || 
            NormalizarTexto(ns).Contains(NormalizarTexto(r.Nombre))
        )
    ).ToList();

    // AUDITORÍA EN CONSOLA DOCKER
    Console.WriteLine("=============================================================");
    Console.WriteLine($"[🔥 .NET AUDIT] Búsqueda: '{busqueda}' | Objetivo: '{objetivo}'");
    Console.WriteLine($"[🔥 .NET AUDIT] Nombres que mandó la IA: {string.Join(", ", nombresSugeridos)}");
    Console.WriteLine($"[🔥 .NET AUDIT] Recetas en BD: {todasLasRecetas.Count} | Matchearon: {recetasMatcheadas.Count}");
    Console.WriteLine("=============================================================");

    if (!recetasMatcheadas.Any())
    {
        return NotFound("La IA sugirió opciones, pero ninguna coincidió con las recetas de tu base de datos.");
    }

    // 🎯 4. Devolvemos el objeto real unificado con la Base de Datos
    var resultadoFinal = recetasMatcheadas.Select(r => new 
    {
        Id = r.Id,
        Nombre = r.Nombre,
        Descripcion = r.Descripcion,
        TiempoCoccionMin = r.TiempoCoccionMin,
        Dificultad = r.Dificultad,
        Porciones = r.Porciones,
        ImagenUrl = r.ImagenUrl,
        VecesCocinada = r.VecesCocinada,
        Ingredientes = r.Ingredientes,
        Pasos = r.Pasos,
        Electrodomesticos = r.Electrodomesticos,
        
        // 🔥 DATOS REALES TRAÍDOS DE POSTGRES (Los mismos que lee tu detalle):
        Calorias = r.Calorias, 
        Proteinas = r.Proteinas,
        Carbohidratos = r.Carbohidratos,
        Grasas = r.Grasas
    });

    return Ok(resultadoFinal);
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