using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Common.Security;
using Nido.Application.Planificador;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/planificador")]
public sealed class PlanificadorController : ControllerBase
{
    private readonly PlanificadorHandler _handler;

    public PlanificadorController(PlanificadorHandler handler)
        => _handler = handler;

    /// <summary>
    /// Obtiene o crea la semana a partir de fechaInicio, en formato yyyy-MM-dd.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSemana(
        [FromQuery] string fechaInicio,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(fechaInicio, "yyyy-MM-dd", out var fecha))
            return BadRequest(new { message = "fechaInicio debe tener formato yyyy-MM-dd." });

        var result = await _handler.GetSemana(currentUser.HogarId, fecha, ct);
        return Ok(ToResponse(result));
    }

    /// <summary>Agrega un item, receta o texto libre, a un slot del planificador.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(
        [FromBody] AddPlanificadorItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(request.Fecha, "yyyy-MM-dd", out var fecha))
            return BadRequest(new { message = "fecha debe tener formato yyyy-MM-dd." });

        var tiposValidos = new[] { "desayuno", "almuerzo", "cena", "tarea" };
        if (!tiposValidos.Contains(request.TipoComida.ToLowerInvariant()))
            return BadRequest(new { message = "tipoComida debe ser desayuno, almuerzo, cena o tarea." });

        var result = await _handler.AddItem(
            new AddPlanificadorItemCommand(
                currentUser.HogarId,
                currentUser.UsuarioId,
                fecha,
                request.TipoComida.ToLowerInvariant(),
                request.RecetaId,
                request.TituloLibre,
                request.Hora),
            ct);

        return Ok(ToItemResponse(result));
    }

    /// <summary>Edita una receta o tarea ya planificada.</summary>
    [HttpPatch("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        [FromBody] UpdatePlanificadorItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _handler.UpdateItem(
            new UpdatePlanificadorItemCommand(
                id,
                currentUser.HogarId,
                request.RecetaId,
                request.TituloLibre,
                request.Hora),
            ct);

        return result is null ? NotFound() : Ok(ToItemResponse(result));
    }

    /// <summary>Elimina un item del planificador.</summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var deleted = await _handler.DeleteItem(
            new DeletePlanificadorItemCommand(id, currentUser.HogarId), ct);

        return deleted ? NoContent() : NotFound();
    }

    private static object ToItemResponse(PlanificadorItemResult item) => new
    {
        item.Id,
        Fecha = item.Fecha.ToString("yyyy-MM-dd"),
        item.TipoComida,
        item.RecetaId,
        item.RecetaNombre,
        item.ImagenUrl,
        item.TituloLibre,
        item.Hora,
        item.Orden,
        item.CreadoPor,
    };

    private static object ToResponse(PlanificadorSemanaResult semana) => new
    {
        semana.Id,
        FechaInicio = semana.FechaInicio.ToString("yyyy-MM-dd"),
        Items = semana.Items.Select(ToItemResponse).ToList(),
    };
}

public sealed record AddPlanificadorItemRequest(
    string Fecha,
    string TipoComida,
    Guid? RecetaId,
    string? TituloLibre,
    string? Hora);

public sealed record UpdatePlanificadorItemRequest(
    Guid? RecetaId,
    string? TituloLibre,
    string? Hora);
