using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram.Pairing;

public sealed class CompleteTelegramPairingByCodeHandler(
    ITelegramPairingRepository repository,
    ITelegramPairingTokenHasher hasher,
    ITelegramPairingRateLimiter rateLimiter)
{
    public async Task<CompleteTelegramPairingResult> HandleAsync(CompleteTelegramPairingByCodeCommand command, CancellationToken ct)
    {
        if (!await rateLimiter.TryAcquireCodeValidateAsync(command.ChatId, ct))
        {
            throw new TelegramPairingRateLimitExceededException();
        }

        var codeHash = hasher.Hash(command.Code);
        return await repository.CompletePairingByCodeAsync(codeHash, command.ChatId, ct);
    }
}
