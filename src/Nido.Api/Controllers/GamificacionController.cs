using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Gamificacion;
using Nido.Application.Common.Security;
using Nido.Application.Gamificacion;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gamificacion")]
public sealed class GamificacionController : ControllerBase
{
    [HttpGet("progreso")]
    public async Task<IActionResult> GetProgreso(
        [FromServices] GetGamificationProgressHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await handler.Handle(currentUser.UsuarioId, ct);
        return Ok(new GamificacionProgresoResponse(
            result.UsuarioId,
            result.CurrentXp,
            result.CurrentLevel,
            result.NextLevel,
            result.NextThresholdXp,
            result.XpToNextLevel,
            result.HasNextLevel));
    }
}
