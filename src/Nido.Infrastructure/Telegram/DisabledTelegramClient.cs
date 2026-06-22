using Microsoft.Extensions.Logging;
using Nido.Application.Telegram.Client;

namespace Nido.Infrastructure.Telegram;

public sealed class DisabledTelegramClient(ILogger<DisabledTelegramClient> logger) : ITelegramClient
{
    public Task<TelegramSendResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode = null,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken ct = default)
    {
        logger.LogWarning(
            "Telegram send skipped because Telegram:BotToken is not configured. ChatId={ChatId}",
            chatId);

        return Task.FromResult<TelegramSendResult>(
            new TelegramSendResult.Error(
                new TelegramPermanentError("Telegram integration is disabled because Telegram:BotToken is not configured.")));
    }
}
