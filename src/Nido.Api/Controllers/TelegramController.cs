using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Telegram;
using Nido.Application.Common.Security;
using Nido.Application.Telegram.Pairing;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/telegram/pairing")]
public sealed class TelegramController : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> StartPairing(
        [FromServices] StartTelegramPairingHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StartTelegramPairingCommand(currentUser.UsuarioId, currentUser.HogarId),
            cancellationToken);

        return Ok(new StartTelegramPairingResponse(
            result.DeepLinkUrl,
            result.PairingCode,
            result.TokenExpiresAt,
            result.CodeExpiresAt));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromServices] GetTelegramPairingStatusHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetTelegramPairingStatusQuery(currentUser.UsuarioId, currentUser.HogarId),
            cancellationToken);

        return Ok(new TelegramPairingStatusResponse(result.IsLinked, result.ChatId, result.PairedAt));
    }

    [HttpPost("unlink")]
    public async Task<IActionResult> Unlink(
        [FromServices] UnlinkTelegramPairingHandler unlinkHandler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await unlinkHandler.HandleAsync(
            new UnlinkTelegramPairingCommand(currentUser.UsuarioId, currentUser.HogarId),
            cancellationToken);

        return Ok(new UnlinkTelegramChatResponse(result.ChatId, result.UnpairedAt));
    }
}
