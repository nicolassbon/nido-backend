using System;

namespace Nido.Application.Telegram.Messaging;

public sealed record TelegramMessageResult(
    Guid MessageId,
    long ChatId,
    string MessageType,
    string PayloadJson,
    TelegramOutboxStatus Status,
    int Attempts,
    DateTime? NextAttemptAt,
    DateTime CreatedAt);
