namespace Nido.Application.Telegram.Pairing;

public interface ITelegramPairingRepository
{
    Task<TelegramPairingTokenResult> CreatePairingTokenAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken ct);

    Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime tokenExpiresAt,
        string codeHash,
        DateTime codeExpiresAt,
        CancellationToken ct);

    Task<CompleteTelegramPairingResult> CompletePairingAsync(
        string tokenHash,
        long chatId,
        CancellationToken ct);

    Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(
        string codeHash,
        long chatId,
        CancellationToken ct);

    Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct);
}
