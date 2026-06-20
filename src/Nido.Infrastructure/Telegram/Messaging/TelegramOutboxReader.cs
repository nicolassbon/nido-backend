using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramOutboxReader(NidoDbContext db, TelegramOptions options) : ITelegramOutboxReader
{
    public async Task<IReadOnlyList<TelegramOutboxMessageLease>> DequeuePendingAsync(int batchSize, DateTime utcNow, CancellationToken ct)
    {
        var lockUntil = utcNow.AddSeconds(Math.Max(options.TimeoutSeconds * 2, options.InteractiveOutboxPollIntervalSeconds + 5));
        FormattableString sql = $@"
                WITH candidate AS (
                    SELECT id
                    FROM telegram_outbox_messages
                    WHERE status = {(int)TelegramOutboxStatus.Pending}
                      AND (next_attempt_at IS NULL OR next_attempt_at <= {utcNow})
                      AND (locked_until IS NULL OR locked_until <= {utcNow})
                    ORDER BY created_at
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE telegram_outbox_messages AS message
                SET locked_until = {lockUntil}
                FROM candidate
                WHERE message.id = candidate.id
                RETURNING message.*";

        var rows = await db.TelegramOutboxMessages
            .FromSqlInterpolated(sql)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .Select(row => new TelegramOutboxMessageLease(
                row.Id,
                row.HogarId,
                row.ChatId,
                row.MessageType,
                row.PayloadJson,
                row.Attempts,
                row.CreatedAt))
            .ToList();
    }

    public Task MarkSentAsync(Guid messageId, int attempts, CancellationToken ct)
        => UpdateAsync(messageId, (int)TelegramOutboxStatus.Sent, attempts, null, null, ct);

    public Task MarkRetryAsync(Guid messageId, DateTime nextAttemptAt, int attempts, CancellationToken ct)
        => UpdateAsync(messageId, (int)TelegramOutboxStatus.Pending, attempts, nextAttemptAt, null, ct);

    public Task MarkFailedAsync(Guid messageId, TelegramOutboxStatus status, int attempts, CancellationToken ct)
        => UpdateAsync(messageId, (int)status, attempts, null, null, ct);

    private async Task UpdateAsync(Guid messageId, int status, int? attempts, DateTime? nextAttemptAt, DateTime? lockedUntil, CancellationToken ct)
    {
        var entity = await db.TelegramOutboxMessages.SingleAsync(x => x.Id == messageId, ct);
        entity.Status = status;
        entity.LockedUntil = lockedUntil;
        entity.NextAttemptAt = nextAttemptAt;

        if (attempts.HasValue)
        {
            entity.Attempts = attempts.Value;
        }

        if (entity.BatchId.HasValue)
        {
            var batchStatus = status switch
            {
                (int)TelegramOutboxStatus.Sent => (int)TelegramBatchStatus.Sent,
                (int)TelegramOutboxStatus.Failed => (int)TelegramBatchStatus.Failed,
                (int)TelegramOutboxStatus.Dead => (int)TelegramBatchStatus.Dead,
                _ => (int?)null
            };

            if (batchStatus.HasValue)
            {
                var batch = await db.TelegramBatches.FindAsync([entity.BatchId.Value], ct);
                if (batch != null)
                {
                    batch.Status = batchStatus.Value;
                }

                var batchMessages = await db.TelegramOutboxMessages
                    .Where(m => m.BatchId == entity.BatchId.Value && m.Id != messageId)
                    .ToListAsync(ct);
                foreach (var bm in batchMessages)
                {
                    bm.Status = status;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
