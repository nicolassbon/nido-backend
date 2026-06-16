using System;

namespace Nido.Application.Telegram.Messaging;

public sealed record EnqueueTelegramMessageRequest(
    Guid HogarId,
    long ChatId,
    string MessageType,
    string PayloadJson,
    DateTime? ScheduledFor = null);

public sealed record TelegramBatchResult(
    Guid BatchId,
    int MessageCount,
    TelegramBatchStatus Status,
    int Attempts,
    DateTime? NextAttemptAt,
    DateTime CreatedAt);
