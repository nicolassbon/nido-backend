using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Npgsql;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramOutboxWriter(
    NidoDbContext db,
    ITelegramOutboxWakeupService wakeupService,
    TelegramOptions options,
    ILogger<TelegramOutboxWriter> logger) : ITelegramOutboxWriter
{
    private static readonly Meter Meter = new("Nido.Telegram.Outbox");
    private static readonly Counter<long> EnqueuedCounter = Meter.CreateCounter<long>("telegram_outbox_enqueued");
    private static readonly Counter<long> DeduplicatedCounter = Meter.CreateCounter<long>("telegram_outbox_deduplicated");

    public async Task<TelegramMessageResult> EnqueueAsync(
        EnqueueTelegramMessageRequest request,
        CancellationToken ct = default)
    {
        if (!options.HasBotToken)
        {
            logger.LogWarning("Telegram bot token is not configured. Ignoring outbox message.");
            return new TelegramMessageResult(
                Guid.Empty,
                request.ChatId,
                request.MessageType,
                request.PayloadJson,
                TelegramOutboxStatus.Failed,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow);
        }

        var entity = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = request.HogarId,
            ChatId = request.ChatId,
            MessageType = request.MessageType,
            PayloadJson = request.PayloadJson,
            Status = (int)TelegramOutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = request.ScheduledFor ?? DateTime.UtcNow,
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

            DeduplicatedCounter.Add(1);
            logger.LogInformation(
                "Telegram outbox deduplicated pending message for chat {ChatId} type {MessageType}.",
                request.ChatId,
                request.MessageType);

            return Map(existing);
        }

        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("NOTIFY telegram_outbox_channel", ct);
        }

        wakeupService.TriggerWakeup();

        EnqueuedCounter.Add(1);
        logger.LogInformation(
            "Telegram outbox enqueued message {MessageId} for chat {ChatId} type {MessageType}.",
            entity.Id,
            entity.ChatId,
            entity.MessageType);

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
