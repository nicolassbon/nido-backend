using System.Security.Cryptography;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram.Pairing;

public sealed class StartTelegramPairingHandler(
    ITelegramPairingRepository repository,
    ITelegramHogarAccess hogarAccess,
    ITelegramPairingTokenHasher hasher,
    ITelegramPairingRateLimiter rateLimiter,
    TelegramOptions options)
{
    private const int PairingCodeLength = 6;
    private const int MaxCreateArtifactsAttempts = 3;

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

        if (!await hogarAccess.IsUserCurrentMemberAsync(command.UsuarioId, command.HogarId, ct))
        {
            throw new TelegramHogarAccessDeniedException();
        }

        var rawToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var tokenHash = hasher.Hash(rawToken);
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(options.PairingTokenTtlMinutes);
        var codeExpiresAt = DateTime.UtcNow.AddMinutes(options.PairingCodeTtlMinutes);

        for (var attempt = 1; attempt <= MaxCreateArtifactsAttempts; attempt++)
        {
            var rawCode = GeneratePairingCode();
            var codeHash = hasher.Hash(rawCode);

            try
            {
                await repository.CreatePairingArtifactsAsync(
                    command.HogarId,
                    command.UsuarioId,
                    tokenHash,
                    tokenExpiresAt,
                    codeHash,
                    codeExpiresAt,
                    ct);

                return new StartTelegramPairingResult(
                    $"https://t.me/{options.BotUsername}?start={rawToken}",
                    rawCode,
                    tokenExpiresAt,
                    codeExpiresAt);
            }
            catch (TelegramPairingCodeCollisionException)
            {
                if (attempt >= MaxCreateArtifactsAttempts)
                {
                    throw new TelegramPairingCodeUnavailableException();
                }
            }
        }

        throw new TelegramPairingCodeUnavailableException();
    }

    private static string GeneratePairingCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString($"D{PairingCodeLength}", System.Globalization.CultureInfo.InvariantCulture);
    }
}
