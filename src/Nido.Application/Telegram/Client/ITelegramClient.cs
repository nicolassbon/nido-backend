namespace Nido.Application.Telegram.Client;

public interface ITelegramClient
{
    Task<TelegramSendResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode = null,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken ct = default);
}
