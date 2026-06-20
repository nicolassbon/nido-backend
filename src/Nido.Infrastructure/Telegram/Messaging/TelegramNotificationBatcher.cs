using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramNotificationBatcher : ITelegramNotificationBatcher
{
    private readonly NidoDbContext _dbContext;
    private readonly ITelegramOutboxWriter _outboxWriter;
    private readonly ITelegramOutboxWakeupService _wakeupService;
    private readonly TelegramOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelegramNotificationBatcher(
        NidoDbContext dbContext,
        ITelegramOutboxWriter outboxWriter,
        ITelegramOutboxWakeupService wakeupService,
        TelegramOptions options)
    {
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
        _wakeupService = wakeupService;
        _options = options;
    }

    public async Task EnqueueEventAsync(
        Guid hogarId,
        long chatId,
        string messageType,
        string payloadJson,
        bool isCritical,
        CancellationToken ct = default)
    {
        if (!_options.HasBotToken)
        {
            return;
        }

        if (isCritical)
        {
            await _outboxWriter.EnqueueAsync(new EnqueueTelegramMessageRequest(
                hogarId,
                chatId,
                messageType,
                payloadJson
            ), ct);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        // Find an active batch (status = 0 - Pending) containing any message for this HogarId and ChatId.
        // We use FOR UPDATE to lock the batch rows associated with these messages.
        var activeBatch = await _dbContext.TelegramBatches
            .FromSqlRaw(@"
                SELECT b.* FROM telegram_batches b
                INNER JOIN telegram_outbox_messages m ON m.batch_id = b.id
                WHERE b.status = 0 AND m.hogar_id = {0} AND m.chat_id = {1}
                LIMIT 1
                FOR UPDATE", hogarId, chatId)
            .FirstOrDefaultAsync(ct);

        if (activeBatch == null)
        {
            activeBatch = new TelegramBatch
            {
                Id = Guid.NewGuid(),
                Status = (int)TelegramBatchStatus.Pending,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.TelegramBatches.Add(activeBatch);
            await _dbContext.SaveChangesAsync(ct);
        }

        var message = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            ChatId = chatId,
            MessageType = messageType,
            PayloadJson = payloadJson,
            Status = (int)TelegramOutboxStatus.Ready, // Ready = 1, will not be processed by worker individually
            Attempts = 0,
            BatchId = activeBatch.Id,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TelegramOutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync(ct);

        // Count how many messages are in this batch
        var messageCount = await _dbContext.TelegramOutboxMessages
            .CountAsync(m => m.BatchId == activeBatch.Id, ct);

        var elapsedMinutes = (DateTime.UtcNow - activeBatch.CreatedAt).TotalMinutes;
        var threshold = _options.GroupingEarlySendThreshold <= 0 ? 5 : _options.GroupingEarlySendThreshold;
        var window = _options.GroupingWindowMinutes <= 0 ? 15 : _options.GroupingWindowMinutes;

        var shouldClose = messageCount >= threshold || elapsedMinutes >= window;

        if (shouldClose)
        {
            await CloseBatchAsyncInternal(_dbContext, activeBatch, ct);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task ProcessExpiredBatchesAsync(CancellationToken ct = default)
    {
        var window = _options.GroupingWindowMinutes <= 0 ? 15 : _options.GroupingWindowMinutes;
        var cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(window));

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        // Find expired pending batches using FOR UPDATE SKIP LOCKED to handle concurrency across replicas
        var expiredBatches = await _dbContext.TelegramBatches
            .FromSqlRaw(@"
                SELECT * FROM telegram_batches
                WHERE status = 0 AND created_at <= {0}
                FOR UPDATE SKIP LOCKED", cutoff)
            .ToListAsync(ct);

        foreach (var batch in expiredBatches)
        {
            await CloseBatchAsyncInternal(_dbContext, batch, ct);
        }

        await transaction.CommitAsync(ct);
    }

    private async Task CloseBatchAsyncInternal(NidoDbContext db, TelegramBatch batch, CancellationToken ct)
    {
        var batchMessages = await db.TelegramOutboxMessages
            .Where(m => m.BatchId == batch.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        if (batchMessages.Count == 0)
        {
            batch.Status = (int)TelegramBatchStatus.Ready;
            await db.SaveChangesAsync(ct);
            return;
        }

        var texts = new List<string>();
        foreach (var msg in batchMessages)
        {
            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(msg.PayloadJson);
                if (doc.RootElement.TryGetProperty("text", out var textProp))
                {
                    text = textProp.GetString();
                }
            }
            catch
            {
                // Fallback to raw json
            }

            text ??= msg.PayloadJson;
            if (!string.IsNullOrWhiteSpace(text))
            {
                texts.Add(text);
            }
        }

        string consolidatedText;
        if (texts.Count == 1)
        {
            consolidatedText = texts[0];
        }
        else
        {
            consolidatedText = string.Join("\n", texts.Select(t => $"• {t}"));
        }

        var payloadJson = JsonSerializer.Serialize(new { text = consolidatedText }, JsonOptions);

        // Update batch status to Ready (1)
        batch.Status = (int)TelegramBatchStatus.Ready;

        // Create consolidated outbox message
        var consolidatedMessage = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = batchMessages[0].HogarId,
            ChatId = batchMessages[0].ChatId,
            MessageType = $"Batch_{batch.Id:N}",
            PayloadJson = payloadJson,
            Status = (int)TelegramOutboxStatus.Pending, // 0 - Pending, to be picked up by sender worker
            Attempts = 0,
            NextAttemptAt = DateTime.UtcNow,
            BatchId = batch.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.TelegramOutboxMessages.Add(consolidatedMessage);
        await db.SaveChangesAsync(ct);

        // Signal wakeup
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("NOTIFY telegram_outbox_channel", ct);
        }
        _wakeupService.TriggerWakeup();
    }
}
