namespace Nido.Application.Telegram.Pairing;

public sealed class UnlinkTelegramPairingHandler(ITelegramPairingRepository repository)
{
    public Task<UnlinkTelegramChatResult> HandleAsync(UnlinkTelegramPairingCommand command, CancellationToken ct)
        => repository.UnlinkActiveLinkAsync(command.UsuarioId, command.HogarId, ct);
}
