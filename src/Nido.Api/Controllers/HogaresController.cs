using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Hogares;
using Nido.Application.Common.Security;
using Nido.Application.Hogares;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("hogares")]
public sealed class HogaresController : ControllerBase
{
    [HttpPost("invitar")]
    public async Task<IActionResult> InvitarConviviente(
        [FromBody] InvitarConviventeRequest request,
        [FromServices] InvitarConviventeHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var token = await handler.Handle(new InvitarConviventeCommand(
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

        var response = miembros.Select(m => new MiembroResponse(m.UsuarioId, m.Nombre, m.Email, m.Rol, m.FotoUrl));
        return Ok(response);
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
}
