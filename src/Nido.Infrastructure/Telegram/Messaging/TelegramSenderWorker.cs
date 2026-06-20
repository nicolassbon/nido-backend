using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramSenderWorker(
    ITelegramOutboxReader outboxReader,
    ITelegramClient telegramClient,
    ITelegramOutboxWakeupService wakeupService,
    TelegramOptions options,
    ILogger<TelegramSenderWorker> logger,
    TimeProvider? timeProvider = null) : BackgroundService
{
    private static readonly Meter Meter = new("Nido.Telegram.Outbox");
    private static readonly Counter<long> SentCounter = Meter.CreateCounter<long>("telegram_outbox_sent");
    private static readonly Counter<long> RetriedCounter = Meter.CreateCounter<long>("telegram_outbox_retried");
    private static readonly Counter<long> DeadCounter = Meter.CreateCounter<long>("telegram_outbox_dead");
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Telegram Sender Worker is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);

                // Wait until notified or polling fallback ticks
                await wakeupService.WaitForMessageAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing Telegram Sender Worker loop. Retrying in 5 seconds...");
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

        logger.LogInformation("Telegram Sender Worker has stopped.");
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var leases = await outboxReader.DequeuePendingAsync(options.OutboxMaxBatchSize, utcNow, ct);

        if (leases.Count > 0)
        {
            logger.LogInformation("Telegram sender worker claimed {MessageCount} outbox message(s).", leases.Count);
        }

        foreach (var lease in leases)
        {
            var text = lease.PayloadJson;
            string? parseMode = null;
            TelegramInlineKeyboardMarkup? replyMarkup = null;

            try
            {
                using var doc = JsonDocument.Parse(lease.PayloadJson);
                if (TryGetProperty(doc.RootElement, out var textProp, "text", "Text")
                    && textProp.ValueKind == JsonValueKind.String)
                {
                    text = textProp.GetString() ?? lease.PayloadJson;
                }

                if (TryGetProperty(doc.RootElement, out var parseModeProp, "parse_mode", "parseMode", "ParseMode")
                    && parseModeProp.ValueKind == JsonValueKind.String)
                {
                    parseMode = parseModeProp.GetString();
                }

                if (TryGetProperty(doc.RootElement, out var markupProp, "reply_markup", "replyMarkup", "ReplyMarkup"))
                {
                    replyMarkup = JsonSerializer.Deserialize<TelegramInlineKeyboardMarkup>(markupProp.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });
                }
            }
            catch (JsonException)
            {
            }

            var result = await telegramClient.SendMessageAsync(lease.ChatId, text, parseMode ?? options.DefaultParseMode, replyMarkup, ct: ct);
            var attempts = lease.Attempts + 1;
            var maxAttempts = GetMaxAttempts(lease.MessageType);

            switch (result)
            {
                case TelegramSendResult.Success:
                    await outboxReader.MarkSentAsync(lease.MessageId, attempts, ct);
                    SentCounter.Add(1);
                    logger.LogInformation(
                        "Telegram outbox message {MessageId} sent successfully. chat_id={ChatId} type={MessageType} attempts={Attempts}",
                        lease.MessageId,
                        lease.ChatId,
                        lease.MessageType,
                        attempts);
                    break;
                case TelegramSendResult.Error { Value: TelegramRateLimitError rateLimit }:
                    var retryAfter = rateLimit.RetryAfter > 0 ? rateLimit.RetryAfter : 10;
                    await ScheduleRetryAsync(lease, attempts, maxAttempts, TimeSpan.FromSeconds(retryAfter), ct);
                    break;
                case TelegramSendResult.Error { Value: TelegramPermanentError } or TelegramSendResult.Error { Value: TelegramValidationError }:
                    await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, attempts, ct);
                    DeadCounter.Add(1);
                    logger.LogWarning(
                        "Telegram outbox message {MessageId} dead-lettered after permanent error. chat_id={ChatId} type={MessageType} attempts={Attempts}",
                        lease.MessageId,
                        lease.ChatId,
                        lease.MessageType,
                        attempts);
                    break;
                case TelegramSendResult.Error { Value: TelegramTransientError }:
                    var backoffSeconds = Math.Pow(2, attempts) * 5;
                    await ScheduleRetryAsync(lease, attempts, maxAttempts, TimeSpan.FromSeconds(backoffSeconds), ct);
                    break;
                case TelegramSendResult.Error:
                    await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, attempts, ct);
                    DeadCounter.Add(1);
                    break;
            }
        }
    }

    private int GetMaxAttempts(string messageType)
        => messageType.StartsWith("interactive.", StringComparison.OrdinalIgnoreCase)
            ? options.OutboxMaxInteractiveAttempts
            : options.MaxAttempts;

    private static bool TryGetProperty(JsonElement element, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private async Task ScheduleRetryAsync(TelegramOutboxMessageLease lease, int attempts, int maxAttempts, TimeSpan delay, CancellationToken ct)
    {
        if (attempts >= maxAttempts)
        {
            await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Failed, attempts, ct);
            DeadCounter.Add(1);
            logger.LogError(
                "Telegram outbox message {MessageId} reached max attempts. chat_id={ChatId} type={MessageType} attempts={Attempts}",
                lease.MessageId,
                lease.ChatId,
                lease.MessageType,
                attempts);
            return;
        }

        var nextAttemptAt = _timeProvider.GetUtcNow().UtcDateTime.Add(delay);
        await outboxReader.MarkRetryAsync(lease.MessageId, nextAttemptAt, attempts, ct);
        RetriedCounter.Add(1);
        logger.LogInformation(
            "Telegram outbox message {MessageId} scheduled for retry at {NextAttemptAt}. chat_id={ChatId} type={MessageType} attempts={Attempts}",
            lease.MessageId,
            nextAttemptAt,
            lease.ChatId,
            lease.MessageType,
            attempts);
    }
}
