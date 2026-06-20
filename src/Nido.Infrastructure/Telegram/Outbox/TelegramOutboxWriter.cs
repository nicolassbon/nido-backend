using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Npgsql;

namespace Nido.Infrastructure.Telegram.Outbox;

public sealed class TelegramOutboxWriter(NidoDbContext db) : ITelegramOutboxWriter
{
    public async Task<TelegramMessageResult> EnqueueAsync(EnqueueTelegramMessageRequest request, CancellationToken ct)
    {
        var entity = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = request.HogarId,
            ChatId = request.ChatId,
            MessageType = request.MessageType,
            PayloadJson = request.PayloadJson,
            Status = (int)TelegramOutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = request.ScheduledFor,
            CreatedAt = DateTime.UtcNow
        };

        db.TelegramOutboxMessages.Add(entity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_telegram_outbox_messages_pending" })
        {
            db.Entry(entity).State = EntityState.Detached;
            var existing = await db.TelegramOutboxMessages.AsNoTracking().SingleAsync(
                x => x.HogarId == request.HogarId
                    && x.ChatId == request.ChatId
                    && x.MessageType == request.MessageType
                    && x.Status == (int)TelegramOutboxStatus.Pending,
                ct);

            return Map(existing);
        }

        return Map(entity);
    }

    private static TelegramMessageResult Map(TelegramOutboxMessage entity)
        => new(
            entity.Id,
            entity.ChatId,
            entity.MessageType,
            entity.PayloadJson,
            (TelegramOutboxStatus)entity.Status,
            entity.Attempts,
            entity.NextAttemptAt,
            entity.CreatedAt);
}
