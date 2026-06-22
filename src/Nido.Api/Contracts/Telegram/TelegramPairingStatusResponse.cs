namespace Nido.Api.Contracts.Telegram;

public sealed record TelegramPairingStatusResponse(
    bool IsLinked,
    long? ChatId,
    DateTime? PairedAt);
