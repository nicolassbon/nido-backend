using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Telegram.Messaging;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramSenderWorkerTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenSendSucceeds_MarksMessageSent()
    {
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 301, "interactive.menu", Payload("ok"), 0, DateTime.UtcNow));
        var worker = CreateWorker(reader, new TelegramSendResult.Success(new TelegramMessageSent(10)));

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.SentMessageId);
        Assert.Equal(1, reader.SentAttempts);
        Assert.Null(reader.RetryMessageId);
        Assert.Null(reader.FailedMessageId);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenRateLimited_SchedulesRetryUsingRetryAfter()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 302, "interactive.menu", Payload("retry"), 0, now.UtcDateTime));
        var worker = CreateWorker(reader, new TelegramSendResult.Error(new TelegramRateLimitError("slow down", 60)), timeProvider);

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.RetryMessageId);
        Assert.NotNull(reader.NextAttemptAt);
        Assert.True(reader.NextAttemptAt >= now.UtcDateTime.AddSeconds(60));
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPermanentFailure_DeadLettersImmediately()
    {
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 303, "interactive.menu", Payload("dead"), 0, DateTime.UtcNow));
        var worker = CreateWorker(reader, new TelegramSendResult.Error(new TelegramPermanentError("bad request")));

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.FailedMessageId);
        Assert.Equal(TelegramOutboxStatus.Dead, reader.FailedStatus);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenTransientFailureExhaustsAttempts_MarksFailed()
    {
        var options = new TelegramOptions { MaxAttempts = 3 };
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 304, "interactive.menu", Payload("retry"), 2, DateTime.UtcNow));
        var worker = CreateWorker(reader, new TelegramSendResult.Error(new TelegramTransientError("timeout")), options: options);

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.FailedMessageId);
        Assert.Equal(TelegramOutboxStatus.Failed, reader.FailedStatus);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPayloadUsesPascalCase_UsesPayloadTextAndParseMode()
    {
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 305, "interactive.menu", JsonSerializer.Serialize(new { Text = "menu", ParseMode = "HTML" }), 0, DateTime.UtcNow));
        var client = new FakeTelegramClient(new TelegramSendResult.Success(new TelegramMessageSent(42)));
        var worker = CreateWorker(reader, client);

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal("menu", client.LastText);
        Assert.Equal("HTML", client.LastParseMode);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenInteractiveFailure_ReachesInteractiveAttemptLimit()
    {
        var options = new TelegramOptions { MaxAttempts = 5, OutboxMaxInteractiveAttempts = 2 };
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 306, "interactive.menu", Payload("retry"), 1, DateTime.UtcNow));
        var worker = CreateWorker(reader, new TelegramSendResult.Error(new TelegramTransientError("timeout")), options: options);

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.FailedMessageId);
        Assert.Equal(TelegramOutboxStatus.Failed, reader.FailedStatus);
    }

    private static TelegramSenderWorker CreateWorker(
        FakeOutboxReader reader,
        TelegramSendResult sendResult,
        FakeTimeProvider? timeProvider = null,
        TelegramOptions? options = null)
        => new(
            reader,
            new FakeTelegramClient(sendResult),
            new FakeTelegramOutboxWakeupService(),
            options ?? new TelegramOptions { InteractiveOutboxPollIntervalSeconds = 2, MaxAttempts = 3 },
            NullLogger<TelegramSenderWorker>.Instance,
            timeProvider ?? new FakeTimeProvider(DateTimeOffset.UtcNow));

    private static TelegramSenderWorker CreateWorker(
        FakeOutboxReader reader,
        FakeTelegramClient client,
        FakeTimeProvider? timeProvider = null,
        TelegramOptions? options = null)
        => new(
            reader,
            client,
            new FakeTelegramOutboxWakeupService(),
            options ?? new TelegramOptions { InteractiveOutboxPollIntervalSeconds = 2, MaxAttempts = 3 },
            NullLogger<TelegramSenderWorker>.Instance,
            timeProvider ?? new FakeTimeProvider(DateTimeOffset.UtcNow));

    private static string Payload(string text)
        => JsonSerializer.Serialize(new { text = text, parseMode = "MarkdownV2" });

    private sealed class FakeOutboxReader(TelegramOutboxMessageLease lease) : ITelegramOutboxReader
    {
        public TelegramOutboxMessageLease Lease { get; } = lease;
        public Guid? SentMessageId { get; private set; }
        public Guid? RetryMessageId { get; private set; }
        public Guid? FailedMessageId { get; private set; }
        public TelegramOutboxStatus FailedStatus { get; private set; }
        public int? SentAttempts { get; private set; }
        public DateTime? NextAttemptAt { get; private set; }

        public Task<IReadOnlyList<TelegramOutboxMessageLease>> DequeuePendingAsync(int batchSize, DateTime utcNow, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<TelegramOutboxMessageLease>>([Lease]);

        public Task MarkFailedAsync(Guid messageId, TelegramOutboxStatus status, int attempts, CancellationToken ct)
        {
            FailedMessageId = messageId;
            FailedStatus = status;
            return Task.CompletedTask;
        }

        public Task MarkRetryAsync(Guid messageId, DateTime nextAttemptAt, int attempts, CancellationToken ct)
        {
            RetryMessageId = messageId;
            NextAttemptAt = nextAttemptAt;
            return Task.CompletedTask;
        }

        public Task MarkSentAsync(Guid messageId, int attempts, CancellationToken ct)
        {
            SentMessageId = messageId;
            SentAttempts = attempts;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTelegramClient(TelegramSendResult result) : ITelegramClient
    {
        public string? LastText { get; private set; }
        public string? LastParseMode { get; private set; }

        public Task<TelegramSendResult> SendMessageAsync(long chatId, string text, string? parseMode = null, TelegramInlineKeyboardMarkup? replyMarkup = null, CancellationToken ct = default)
        {
            LastText = text;
            LastParseMode = parseMode;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeTelegramOutboxWakeupService : ITelegramOutboxWakeupService
    {
        public void TriggerWakeup() { }
        public Task WaitForMessageAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
