using System.Security.Cryptography;
using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram.Pairing;

public sealed class StartTelegramPairingHandler(
    ITelegramPairingRepository repository,
    ITelegramPairingTokenHasher hasher,
    ITelegramPairingRateLimiter rateLimiter,
    TelegramOptions options)
{
    public async Task<StartTelegramPairingResult> HandleAsync(StartTelegramPairingCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.BotUsername))
        {
            throw new TelegramConfigurationException("Telegram:BotUsername is required for Telegram pairing.");
        }

        if (!await rateLimiter.TryAcquireGenerateAsync(command.UsuarioId, ct))
        {
            throw new TelegramPairingRateLimitExceededException();
        }

        var rawToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var tokenHash = hasher.Hash(rawToken);
        var expiresAt = DateTime.UtcNow.AddMinutes(options.PairingTokenTtlMinutes);

        await repository.CreatePairingTokenAsync(command.HogarId, command.UsuarioId, tokenHash, expiresAt, ct);

        return new StartTelegramPairingResult(
            $"https://t.me/{options.BotUsername}?start={rawToken}",
            expiresAt);
    }
}
