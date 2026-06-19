namespace Nido.Application.Telegram.Client;

public sealed record TelegramInlineKeyboardButton(string Text, string CallbackData);

public sealed record TelegramInlineKeyboardMarkup(IReadOnlyList<IReadOnlyList<TelegramInlineKeyboardButton>> InlineKeyboard);
