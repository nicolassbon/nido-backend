using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramOutboxWriter(NidoDbContext dbContext, ITelegramOutboxWakeupService wakeupService) : ITelegramOutboxWriter
{
    public async Task<TelegramMessageResult> EnqueueAsync(
        EnqueueTelegramMessageRequest request,
        CancellationToken ct = default)
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
            NextAttemptAt = request.ScheduledFor ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TelegramOutboxMessages.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlRawAsync("NOTIFY telegram_outbox_channel", ct);
        }

        wakeupService.TriggerWakeup();

        return new TelegramMessageResult(
            entity.Id,
            entity.ChatId,
            entity.MessageType,
            entity.PayloadJson,
            (TelegramOutboxStatus)entity.Status,
            entity.Attempts,
            entity.NextAttemptAt,
            entity.CreatedAt);
    }
}
