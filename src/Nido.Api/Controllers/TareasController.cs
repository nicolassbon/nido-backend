using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Tareas;
using Nido.Application.Common.Security;
using Nido.Application.Gamificacion;
using Nido.Application.Tareas;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tareas")]
public sealed class TareasController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTareas(
        [FromServices] GetTareasHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        var tareas = await handler.Handle(new GetTareasQuery(currentUser.HogarId), ct);
        return Ok(tareas.Select(t => ToResponse(t, gamificationRules)));
    }

    [HttpGet("mis-tareas")]
    public async Task<IActionResult> GetMisTareas(
        [FromServices] GetMisTareasHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        var tareas = await handler.Handle(new GetMisTareasQuery(currentUser.HogarId, currentUser.UsuarioId), ct);
        return Ok(tareas.Select(t => ToResponse(t, gamificationRules)));
    }

    private int UtcOffsetMinutes =>
        int.TryParse(Request.Query["utcOffsetMinutes"], out var v) ? v : 0;

    [HttpGet("distribucion-semanal")]
    public async Task<IActionResult> GetDistribucionSemanal(
        [FromServices] GetDistribucionSemanalHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var distribucion = await handler.Handle(new GetDistribucionSemanalQuery(currentUser.HogarId, UtcOffsetMinutes), ct);

        var response = new DistribucionSemanalResponse(
            distribucion.Select(d => new DistribucionDiaResponse(
                d.Dia,
                d.Fecha,
                d.Miembros.Select(m => new MiembroDistribucionResponse(m.UsuarioId, m.Nombre, m.Completadas)).ToList()
            )).ToList());

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTarea(
        [FromBody] CreateTareaRequest request,
        [FromServices] CreateTareaHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        var tarea = await handler.Handle(new CreateTareaCommand(
            currentUser.HogarId,
            currentUser.UsuarioId,
            request.Titulo,
            request.Descripcion,
            request.FechaLimite,
            request.AsignadoA), ct);

        return CreatedAtAction(nameof(GetTareas), ToResponse(tarea, gamificationRules));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateTarea(
        Guid id,
        [FromBody] UpdateTareaRequest request,
        [FromServices] UpdateTareaHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        try
        {
            var tarea = await handler.Handle(new UpdateTareaCommand(
                id,
                currentUser.HogarId,
                request.Titulo,
                request.Descripcion,
                request.FechaLimite,
                request.Estado), ct);

            if (tarea is null) return NotFound();
            return Ok(ToResponse(tarea, gamificationRules));
        }
        catch (InvalidOperationException ex) when (request.Estado == "completada")
        {
            return BadRequest(new ProblemDetails
            {
                Title = "TAREA_ESTADO_INVALIDO",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/completar")]
    public async Task<IActionResult> CompletarTarea(
        Guid id,
        [FromServices] CompletarTareaHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        var tarea = await handler.Handle(new CompletarTareaCommand(id, currentUser.HogarId, currentUser.UsuarioId), ct);
        if (tarea is null) return NotFound();
        return Ok(ToResponse(tarea, gamificationRules));
    }

    [HttpPost("{id:guid}/asignar")]
    public async Task<IActionResult> AsignarTarea(
        Guid id,
        [FromBody] AsignarTareaRequest request,
        [FromServices] AsignarTareaHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IGamificationRulesService gamificationRules,
        CancellationToken ct)
    {
        var tarea = await handler.Handle(new AsignarTareaCommand(id, currentUser.HogarId, request.UsuarioId, currentUser.UsuarioId), ct);
        if (tarea is null) return NotFound();
        return Ok(ToResponse(tarea, gamificationRules));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTarea(
        Guid id,
        [FromServices] DeleteTareaHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var deleted = await handler.Handle(new DeleteTareaCommand(id, currentUser.HogarId), ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private TareaResponse ToResponse(TareaResult t, IGamificationRulesService gamificationRules)
    {
        var localNow = DateTime.UtcNow.AddMinutes(-UtcOffsetMinutes);
        var vencida = t.FechaLimite.HasValue
            && t.FechaLimite.Value.Date < localNow.Date
            && t.Estado != "completada";

        return new TareaResponse(
            t.Id,
            t.Titulo,
            t.Descripcion,
            t.Estado,
            t.FechaLimite,
            t.FechaCompletado,
            t.CreadoPor,
            t.CreadoPorNombre,
            t.CompletadoPor,
            t.CompletadoPorNombre,
            t.AsignadoA is null ? null : new AsignacionResponse(t.AsignadoA.UsuarioId, t.AsignadoA.Nombre, t.AsignadoA.FotoStorageKey),
            vencida,
            t.CreatedAt,
            gamificationRules.TaskXpOtorgado(t.Estado == "completada"));
    }
}
