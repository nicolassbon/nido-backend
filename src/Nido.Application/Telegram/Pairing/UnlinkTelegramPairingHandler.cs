using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram.Pairing;

public sealed class UnlinkTelegramPairingHandler(
    ITelegramPairingRepository repository,
    ITelegramHogarAccess hogarAccess)
{
    public async Task<UnlinkTelegramChatResult> HandleAsync(UnlinkTelegramPairingCommand command, CancellationToken ct)
    {
        if (!await hogarAccess.IsUserCurrentMemberAsync(command.UsuarioId, command.HogarId, ct))
        {
            throw new TelegramHogarAccessDeniedException();
        }

        return await repository.UnlinkActiveLinkAsync(command.UsuarioId, command.HogarId, ct);
    }
}
