using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Telegram.Outbox;

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
    public async Task ProcessPendingAsync_WhenTransientFailureExhaustsAttempts_DeadLetters()
    {
        var options = new TelegramOptions { OutboxMaxInteractiveAttempts = 3 };
        var reader = new FakeOutboxReader(
            new TelegramOutboxMessageLease(Guid.NewGuid(), Guid.NewGuid(), 304, "interactive.menu", Payload("retry"), 2, DateTime.UtcNow));
        var worker = CreateWorker(reader, new TelegramSendResult.Error(new TelegramTransientError("timeout")), options: options);

        await worker.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(reader.Lease.MessageId, reader.FailedMessageId);
        Assert.Equal(TelegramOutboxStatus.Dead, reader.FailedStatus);
    }

    private static TelegramSenderWorker CreateWorker(
        FakeOutboxReader reader,
        TelegramSendResult sendResult,
        FakeTimeProvider? timeProvider = null,
        TelegramOptions? options = null)
        => new(
            reader,
            new FakeTelegramClient(sendResult),
            options ?? new TelegramOptions { InteractiveOutboxPollIntervalSeconds = 2, OutboxMaxInteractiveAttempts = 3 },
            NullLogger<TelegramSenderWorker>.Instance,
            timeProvider ?? new FakeTimeProvider(DateTimeOffset.UtcNow));

    private static string Payload(string text)
        => JsonSerializer.Serialize(new TelegramOutboxPayload(text, "MarkdownV2"));

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
        public Task<TelegramSendResult> SendMessageAsync(long chatId, string text, string? parseMode = null, TelegramInlineKeyboardMarkup? replyMarkup = null, CancellationToken ct = default)
            => Task.FromResult(result);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
