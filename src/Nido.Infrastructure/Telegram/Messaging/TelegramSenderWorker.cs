using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramSenderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramOutboxWakeupService _wakeupService;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramSenderWorker> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelegramSenderWorker(
        IServiceProvider serviceProvider,
        ITelegramOutboxWakeupService wakeupService,
        TelegramOptions options,
        ILogger<TelegramSenderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _wakeupService = wakeupService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Sender Worker is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);

                // Wait until notified or polling fallback ticks
                await _wakeupService.WaitForMessageAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Telegram Sender Worker loop. Retrying in 5 seconds...");
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Telegram Sender Worker has stopped.");
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        var limit = _options.OutboxMaxBatchSize <= 0 ? 50 : _options.OutboxMaxBatchSize;
        var now = DateTime.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<ITelegramClient>();

        // Lock pending messages for update
        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);

        var messages = await db.TelegramOutboxMessages
            .FromSqlRaw(@"
                SELECT * FROM telegram_outbox_messages
                WHERE status = 0
                  AND (next_attempt_at IS NULL OR next_attempt_at <= {0})
                  AND (locked_until IS NULL OR locked_until <= {0})
                ORDER BY created_at
                LIMIT {1}
                FOR UPDATE SKIP LOCKED", now, limit)
            .ToListAsync(stoppingToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(stoppingToken);
            return;
        }

        var lockTime = now.AddMinutes(5);
        foreach (var msg in messages)
        {
            msg.LockedUntil = lockTime;
        }

        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);

        _logger.LogInformation("Processing {Count} Telegram outbox messages...", messages.Count);

        foreach (var message in messages)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                string text;
                TelegramInlineKeyboardMarkup? replyMarkup = null;

                try
                {
                    using var doc = JsonDocument.Parse(message.PayloadJson);
                    if (doc.RootElement.TryGetProperty("text", out var textProp))
                    {
                        text = textProp.GetString() ?? message.PayloadJson;
                    }
                    else
                    {
                        text = message.PayloadJson;
                    }

                    if (doc.RootElement.TryGetProperty("reply_markup", out var markupProp))
                    {
                        replyMarkup = JsonSerializer.Deserialize<TelegramInlineKeyboardMarkup>(markupProp.GetRawText(), JsonOptions);
                    }
                }
                catch (JsonException)
                {
                    text = message.PayloadJson;
                }

                var result = await client.SendMessageAsync(
                    message.ChatId,
                    text,
                    _options.DefaultParseMode,
                    replyMarkup,
                    stoppingToken);

                await using var updateDb = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<NidoDbContext>();
                var dbMessage = await updateDb.TelegramOutboxMessages.FindAsync(message.Id);
                if (dbMessage != null)
                {
                    if (result is TelegramSendResult.Success)
                    {
                        dbMessage.Status = (int)TelegramOutboxStatus.Sent;
                        dbMessage.LockedUntil = null;
                        if (dbMessage.BatchId != null)
                        {
                            var batch = await updateDb.TelegramBatches.FindAsync(dbMessage.BatchId);
                            if (batch != null)
                            {
                                batch.Status = (int)TelegramBatchStatus.Sent;
                            }
                            var batchMessages = await updateDb.TelegramOutboxMessages
                                .Where(m => m.BatchId == dbMessage.BatchId && m.Id != dbMessage.Id)
                                .ToListAsync(stoppingToken);
                            foreach (var bm in batchMessages)
                            {
                                bm.Status = (int)TelegramOutboxStatus.Sent;
                            }
                        }
                        _logger.LogInformation("Telegram message {MessageId} sent successfully.", message.Id);
                    }
                    else if (result is TelegramSendResult.Error errorResult)
                    {
                        var error = errorResult.Value;
                        dbMessage.Attempts++;

                        if (error is TelegramPermanentError || error is TelegramValidationError)
                        {
                            dbMessage.Status = (int)TelegramOutboxStatus.Dead;
                            dbMessage.LockedUntil = null;
                            if (dbMessage.BatchId != null)
                            {
                                var batch = await updateDb.TelegramBatches.FindAsync(dbMessage.BatchId);
                                if (batch != null)
                                {
                                    batch.Status = (int)TelegramBatchStatus.Dead;
                                }
                                var batchMessages = await updateDb.TelegramOutboxMessages
                                    .Where(m => m.BatchId == dbMessage.BatchId && m.Id != dbMessage.Id)
                                    .ToListAsync(stoppingToken);
                                foreach (var bm in batchMessages)
                                {
                                    bm.Status = (int)TelegramOutboxStatus.Dead;
                                }
                            }
                            _logger.LogWarning("Telegram message {MessageId} failed permanently: {Error}", message.Id, error.Description);
                        }
                        else if (error is TelegramRateLimitError rateLimitError)
                        {
                            var retryAfter = rateLimitError.RetryAfter > 0 ? rateLimitError.RetryAfter : 10;
                            dbMessage.NextAttemptAt = DateTime.UtcNow.AddSeconds(retryAfter);
                            dbMessage.LockedUntil = null;
                            _logger.LogWarning("Telegram message {MessageId} rate limited. Retrying after {Seconds}s.", message.Id, retryAfter);
                        }
                        else
                        {
                            if (dbMessage.Attempts >= _options.MaxAttempts)
                            {
                                dbMessage.Status = (int)TelegramOutboxStatus.Failed;
                                if (dbMessage.BatchId != null)
                                {
                                    var batch = await updateDb.TelegramBatches.FindAsync(dbMessage.BatchId);
                                    if (batch != null)
                                    {
                                        batch.Status = (int)TelegramBatchStatus.Failed;
                                    }
                                    var batchMessages = await updateDb.TelegramOutboxMessages
                                        .Where(m => m.BatchId == dbMessage.BatchId && m.Id != dbMessage.Id)
                                        .ToListAsync(stoppingToken);
                                    foreach (var bm in batchMessages)
                                    {
                                        bm.Status = (int)TelegramOutboxStatus.Failed;
                                    }
                                }
                                _logger.LogError("Telegram message {MessageId} exceeded max attempts and failed.", message.Id);
                            }
                            else
                            {
                                var backoffSeconds = Math.Pow(2, dbMessage.Attempts) * 5;
                                dbMessage.NextAttemptAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
                                _logger.LogInformation("Telegram message {MessageId} encountered transient error: {Error}. Retrying in {Seconds}s.", message.Id, error.Description, backoffSeconds);
                            }
                            dbMessage.LockedUntil = null;
                        }
                    }

                    await updateDb.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}.", message.Id);
                try
                {
                    await using var updateDb = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<NidoDbContext>();
                    var dbMessage = await updateDb.TelegramOutboxMessages.FindAsync(message.Id);
                    if (dbMessage != null)
                    {
                        dbMessage.Attempts++;
                        dbMessage.LockedUntil = null;
                        dbMessage.NextAttemptAt = DateTime.UtcNow.AddMinutes(1);
                        await updateDb.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception resetEx)
                {
                    _logger.LogError(resetEx, "Failed to reset lock/state for message {MessageId}.", message.Id);
                }
            }
        }
    }
}
