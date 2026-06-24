using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Hogares;
using Nido.Application.Common.Security;
using Nido.Application.Hogares;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/hogares")]
public sealed class HogaresController : ControllerBase
{
    [HttpPost("invitar")]
    public async Task<IActionResult> InvitarIntegrante(
        [FromBody] InvitarIntegranteRequest request,
        [FromServices] InvitarIntegranteHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var token = await handler.Handle(new InvitarIntegranteCommand(
            currentUser.UsuarioId,
            currentUser.HogarId,
            request.EmailInvitado), cancellationToken);

        return Created(string.Empty, new { token });
    }

    [HttpPost("aceptar-invitacion")]
    public async Task<IActionResult> AceptarInvitacion(
        [FromBody] AceptarInvitacionRequest request,
        [FromServices] AceptarInvitacionHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AceptarInvitacionCommand(
            currentUser.UsuarioId,
            request.Token), cancellationToken);

        return Ok(new AceptarInvitacionResponse(result.HogarId, result.HogarNombre, result.NuevoToken));
    }

    [AllowAnonymous]
    [HttpGet("invitaciones/{token}")]
    public async Task<IActionResult> GetInvitacionPreview(
        [FromRoute] string token,
        [FromServices] GetInvitacionPreviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(token, cancellationToken);
        return Ok(new InvitacionPreviewResponse(result.HogarNombre, result.EmailInvitado, result.ExpiraEn));
    }

    [HttpGet("miembros")]
    public async Task<IActionResult> GetMiembros(
        [FromServices] GetMiembrosHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var miembros = await handler.Handle(new GetMiembrosQuery(
            currentUser.UsuarioId,
            currentUser.HogarId), cancellationToken);

        var response = miembros.Select(m => new MiembroResponse(
            m.UsuarioId,
            m.Nombre,
            m.Email,
            m.Rol,
            m.FotoUrl,
            m.Alergias));
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetHogar(
        [FromServices] GetHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var hogar = await handler.Handle(
            new GetHogarQuery(currentUser.UsuarioId, currentUser.HogarId),
            cancellationToken);

        return Ok(new HogarResponse(hogar.Id, hogar.Nombre));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateHogar(
        [FromBody] UpdateHogarRequest request,
        [FromServices] UpdateHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await handler.Handle(
                new UpdateHogarCommand(currentUser.UsuarioId, currentUser.HogarId, request.Nombre),
                cancellationToken);

            return Ok(new HogarResponse(updated.Id, updated.Nombre));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("miembros/{usuarioId:guid}")]
    public async Task<IActionResult> RemoveMiembro(
        [FromRoute] Guid usuarioId,
        [FromServices] RemoveMiembroHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new RemoveMiembroCommand(
            currentUser.UsuarioId,
            currentUser.HogarId,
            usuarioId), cancellationToken);

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CrearHogar(
        [FromBody] CrearHogarRequest request,
        [FromServices] CrearHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CrearHogarCommand(
            currentUser.UsuarioId,
            request.Nombre), cancellationToken);

        return Created(string.Empty, new CrearHogarResponse(result.HogarId, result.HogarNombre, result.NuevoToken));
    }

    [HttpGet("mis-hogares")]
    public async Task<IActionResult> GetMisHogares(
        [FromServices] GetHogaresHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var hogares = await handler.Handle(new GetHogaresQuery(currentUser.UsuarioId), cancellationToken);
        return Ok(hogares.Select(h => new HogarResumenResponse(h.Id, h.Nombre, h.Rol)));
    }

    [HttpPatch("{hogarId:guid}")]
    public async Task<IActionResult> RenombrarHogar(
        [FromRoute] Guid hogarId,
        [FromBody] UpdateHogarRequest request,
        [FromServices] UpdateHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await handler.Handle(
                new UpdateHogarCommand(currentUser.UsuarioId, hogarId, request.Nombre),
                cancellationToken);

            return Ok(new HogarResponse(updated.Id, updated.Nombre));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{hogarId:guid}")]
    public async Task<IActionResult> EliminarHogar(
        [FromRoute] Guid hogarId,
        [FromServices] EliminarHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new EliminarHogarCommand(
            currentUser.UsuarioId,
            hogarId,
            currentUser.HogarId), cancellationToken);

        return NoContent();
    }

    [HttpPost("{hogarId:guid}/activar")]
    public async Task<IActionResult> CambiarHogar(
        [FromRoute] Guid hogarId,
        [FromServices] CambiarHogarHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CambiarHogarCommand(
            currentUser.UsuarioId,
            hogarId), cancellationToken);

        return Ok(new CambiarHogarResponse(result.HogarId, result.HogarNombre, result.NuevoToken));
    }
}
