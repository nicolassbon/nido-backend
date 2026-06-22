namespace Nido.Application.Telegram.Messaging;

public interface ITelegramOutboxReader
{
    Task<IReadOnlyList<TelegramOutboxMessageLease>> DequeuePendingAsync(int batchSize, DateTime utcNow, CancellationToken ct);
    Task MarkSentAsync(Guid messageId, int attempts, CancellationToken ct);
    Task MarkRetryAsync(Guid messageId, DateTime nextAttemptAt, int attempts, CancellationToken ct);
    Task MarkFailedAsync(Guid messageId, TelegramOutboxStatus status, int attempts, CancellationToken ct);
}

public sealed record TelegramOutboxMessageLease(
    Guid MessageId,
    Guid HogarId,
    long ChatId,
    string MessageType,
    string PayloadJson,
    int Attempts,
    DateTime CreatedAt);

public sealed record TelegramOutboxPayload(string Text, string? ParseMode);
