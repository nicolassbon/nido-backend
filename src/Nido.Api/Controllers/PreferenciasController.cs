using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Preferencias;
using Nido.Application.Common.Security;
using Nido.Application.Preferencias;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/preferencias")]
public sealed class PreferenciasController : ControllerBase
{
    private readonly GetUserPreferencesHandler _getHandler;
    private readonly UpdateUserPreferencesHandler _updateHandler;

    public PreferenciasController(
        GetUserPreferencesHandler getHandler,
        UpdateUserPreferencesHandler updateHandler)
    {
        _getHandler = getHandler;
        _updateHandler = updateHandler;
    }

    [HttpGet("usuario")]
    public async Task<IActionResult> GetPreferencias(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getHandler.Handle(
            new GetUserPreferencesQuery(currentUser.UsuarioId), ct);

        return Ok(new UserPreferencesResponse(result.DiasAlerta));
    }

    [HttpPatch("usuario")]
    public async Task<IActionResult> UpdatePreferencias(
        [FromBody] UpdateUserPreferencesRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _updateHandler.Handle(
            new UpdateUserPreferencesCommand(currentUser.UsuarioId, request.DiasAlerta), ct);

        return Ok(new UserPreferencesResponse(result.DiasAlerta));
    }
}
