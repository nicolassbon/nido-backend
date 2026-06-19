namespace Nido.Application.Telegram.Pairing;

public interface ITelegramPairingRateLimiter
{
    Task<bool> TryAcquireGenerateAsync(Guid usuarioId, CancellationToken ct);

    Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct);
}
