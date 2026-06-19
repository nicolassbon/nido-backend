namespace Nido.Application.Telegram.Pairing;

public interface ITelegramPairingRepository
{
    Task<TelegramPairingTokenResult> CreatePairingTokenAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken ct);

    Task<CompleteTelegramPairingResult> CompletePairingAsync(
        string tokenHash,
        long chatId,
        CancellationToken ct);

    Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct);
}
