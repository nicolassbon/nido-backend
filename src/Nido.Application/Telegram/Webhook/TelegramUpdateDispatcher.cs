using System.Text.RegularExpressions;
using Nido.Application.Telegram.Pairing;

namespace Nido.Application.Telegram.Webhook;

public sealed partial class TelegramUpdateDispatcher(
    CompleteTelegramPairingHandler completePairingHandler,
    CompleteTelegramPairingByCodeHandler completePairingByCodeHandler,
    UnlinkTelegramChatHandler unlinkTelegramChatHandler)
{
    private static readonly Regex PairingCodeRegex = GetPairingCodeRegex();

    public async Task<TelegramDispatchResult?> DispatchAsync(TelegramWebhookRequest request, CancellationToken ct)
    {
        var text = request.Message?.Text?.Trim();
        var chatId = request.Message?.Chat?.Id;

        if (string.IsNullOrWhiteSpace(text) || chatId is null or 0)
        {
            return null;
        }

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var command = parts[0].Split('@', 2)[0];

        if (string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            await completePairingHandler.HandleAsync(new CompleteTelegramPairingCommand(chatId.Value, parts[1]), ct);
            return new TelegramDispatchResult(chatId.Value, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido.");
        }

        if (string.Equals(command, "/pair", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            var code = parts[1];
            if (!PairingCodeRegex.IsMatch(code))
            {
                return null;
            }

            await completePairingByCodeHandler.HandleAsync(new CompleteTelegramPairingByCodeCommand(chatId.Value, code), ct);
            return new TelegramDispatchResult(chatId.Value, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido.");
        }

        if (string.Equals(command, "/unlink", StringComparison.OrdinalIgnoreCase))
        {
            await unlinkTelegramChatHandler.HandleAsync(new UnlinkTelegramChatCommand(chatId.Value), ct);
            return new TelegramDispatchResult(chatId.Value, "Listo. Este chat quedó desvinculado de tu hogar en Nido.");
        }

        return null;
    }

    [GeneratedRegex(@"^\d{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex GetPairingCodeRegex();
}

public sealed record TelegramDispatchResult(long ChatId, string ConfirmationText);
