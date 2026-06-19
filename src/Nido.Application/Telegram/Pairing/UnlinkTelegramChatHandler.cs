namespace Nido.Application.Telegram.Pairing;

public sealed class UnlinkTelegramChatHandler(ITelegramPairingRepository repository)
{
    public Task<UnlinkTelegramChatResult> HandleAsync(UnlinkTelegramChatCommand command, CancellationToken ct)
        => repository.UnlinkChatAsync(command.ChatId, ct);
}
