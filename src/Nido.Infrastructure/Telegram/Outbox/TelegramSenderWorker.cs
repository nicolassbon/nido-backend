using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;

namespace Nido.Infrastructure.Telegram.Outbox;

public sealed class TelegramSenderWorker(
    ITelegramOutboxReader outboxReader,
    ITelegramClient telegramClient,
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
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(options.InteractiveOutboxPollIntervalSeconds), stoppingToken);
        }
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
            var payload = JsonSerializer.Deserialize<TelegramOutboxPayload>(lease.PayloadJson);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Text))
            {
                await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, lease.Attempts + 1, ct);
                DeadCounter.Add(1);
                logger.LogWarning(
                    "Telegram interactive outbox message {MessageId} dead-lettered because payload was invalid. chat_id={ChatId} type={MessageType}",
                    lease.MessageId,
                    lease.ChatId,
                    lease.MessageType);
                continue;
            }

            var result = await telegramClient.SendMessageAsync(lease.ChatId, payload.Text, payload.ParseMode, ct: ct);
            var attempts = lease.Attempts + 1;

            switch (result)
            {
                case TelegramSendResult.Success:
                    await outboxReader.MarkSentAsync(lease.MessageId, attempts, ct);
                    SentCounter.Add(1);
                    logger.LogInformation(
                        "Telegram interactive outbox message {MessageId} sent. chat_id={ChatId} type={MessageType} attempts={Attempts}",
                        lease.MessageId,
                        lease.ChatId,
                        lease.MessageType,
                        attempts);
                    break;
                case TelegramSendResult.Error { Value: TelegramRateLimitError rateLimit }:
                    await ScheduleRetryAsync(lease, attempts, TimeSpan.FromSeconds(Math.Max(rateLimit.RetryAfter, 1)), ct);
                    break;
                case TelegramSendResult.Error { Value: TelegramPermanentError }:
                    await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, attempts, ct);
                    DeadCounter.Add(1);
                    logger.LogWarning(
                        "Telegram interactive outbox message {MessageId} dead-lettered after permanent error. chat_id={ChatId} type={MessageType} attempts={Attempts}",
                        lease.MessageId,
                        lease.ChatId,
                        lease.MessageType,
                        attempts);
                    break;
                case TelegramSendResult.Error { Value: TelegramTransientError }:
                    await ScheduleRetryAsync(lease, attempts, TimeSpan.FromSeconds(Math.Pow(2, attempts)), ct);
                    break;
                case TelegramSendResult.Error:
                    await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, attempts, ct);
                    DeadCounter.Add(1);
                    break;
            }
        }
    }

    private async Task ScheduleRetryAsync(TelegramOutboxMessageLease lease, int attempts, TimeSpan delay, CancellationToken ct)
    {
        if (attempts >= options.OutboxMaxInteractiveAttempts)
        {
            await outboxReader.MarkFailedAsync(lease.MessageId, TelegramOutboxStatus.Dead, attempts, ct);
            DeadCounter.Add(1);
            logger.LogWarning(
                "Telegram interactive outbox message {MessageId} reached max attempts. chat_id={ChatId} type={MessageType} attempts={Attempts}",
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
            "Telegram interactive outbox message {MessageId} scheduled for retry at {NextAttemptAt}. chat_id={ChatId} type={MessageType} attempts={Attempts}",
            lease.MessageId,
            nextAttemptAt,
            lease.ChatId,
            lease.MessageType,
            attempts);
    }
}
