namespace Nido.Application.Telegram.Pairing;

public sealed class GetTelegramPairingStatusHandler(ITelegramPairingRepository repository)
{
    public async Task<TelegramPairingStatusResult> HandleAsync(GetTelegramPairingStatusQuery query, CancellationToken ct)
    {
        var link = await repository.GetActiveLinkForCurrentMemberAsync(query.UsuarioId, query.HogarId, ct);

        return link is null
            ? new TelegramPairingStatusResult(false, null, null)
            : new TelegramPairingStatusResult(true, link.ChatId, link.PairedAt);
    }
}
