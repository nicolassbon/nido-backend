using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram.Pairing;

public sealed class CompleteTelegramPairingHandler(
    ITelegramPairingRepository repository,
    ITelegramPairingTokenHasher hasher,
    ITelegramPairingRateLimiter rateLimiter)
{
    public async Task<CompleteTelegramPairingResult> HandleAsync(CompleteTelegramPairingCommand command, CancellationToken ct)
    {
        if (!await rateLimiter.TryAcquireConsumeAsync(command.ChatId, ct))
        {
            throw new TelegramPairingRateLimitExceededException();
        }

        var tokenHash = hasher.Hash(command.Token);
        return await repository.CompletePairingAsync(tokenHash, command.ChatId, ct);
    }
}
