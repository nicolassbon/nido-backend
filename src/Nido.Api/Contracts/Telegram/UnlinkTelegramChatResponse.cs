namespace Nido.Api.Contracts.Telegram;

public sealed record UnlinkTelegramChatResponse(
    long ChatId,
    DateTime UnpairedAt);
