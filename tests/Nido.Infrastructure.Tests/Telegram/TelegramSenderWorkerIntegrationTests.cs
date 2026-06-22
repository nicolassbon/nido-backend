using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Messaging;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramSenderWorkerIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private ServiceProvider _serviceProvider = null!;
    private NidoDbContext _db = null!;
    private FakeTelegramClient _fakeTelegramClient = null!;
    private FakeTelegramOutboxWakeupService _wakeupService = null!;
    private TelegramSenderWorker _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_sender_worker");

        var services = new ServiceCollection();
        
        services.AddDbContext<NidoDbContext>(options =>
            options.UseNpgsql(_database.ConnectionString)
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        _fakeTelegramClient = new FakeTelegramClient();
        _wakeupService = new FakeTelegramOutboxWakeupService();
        var telegramOptions = new TelegramOptions
        {
            BotToken = "test_token",
            OutboxMaxBatchSize = 10,
            MaxAttempts = 3,
            DefaultParseMode = "MarkdownV2"
        };

        services.AddSingleton<ITelegramClient>(_fakeTelegramClient);
        services.AddSingleton(telegramOptions);

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<NidoDbContext>();
        await _db.Database.MigrateAsync();

        var outboxReader = new TelegramOutboxReader(_db, telegramOptions);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramSenderWorker>.Instance;
        _sut = new TelegramSenderWorker(outboxReader, _fakeTelegramClient, _wakeupService, telegramOptions, logger);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Process_MessageSentSuccessfully_UpdatesStatusToSent()
    {
        var msg = await SeedMessageAsync();

        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
        {
            Assert.Equal(msg.ChatId, chatId);
            Assert.Equal("Test text", text);
            return Task.FromResult<TelegramSendResult>(new TelegramSendResult.Success(new TelegramMessageSent(1001L)));
        };

        var startTask = _sut.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        await WaitForWorkerIterationAsync(msg.Id, m => m.Status == (int)TelegramOutboxStatus.Sent);

        await _sut.StopAsync(CancellationToken.None);
        await startTask;

        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == msg.Id);
        Assert.Equal((int)TelegramOutboxStatus.Sent, stored.Status);
        Assert.Null(stored.LockedUntil);
    }

    [Fact]
    public async Task Process_RateLimitError_SetsNextAttemptAt()
    {
        var msg = await SeedMessageAsync();

        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
            Task.FromResult<TelegramSendResult>(new TelegramSendResult.Error(new TelegramRateLimitError("Limit exceeded", 15)));

        var startTask = _sut.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        await WaitForWorkerIterationAsync(msg.Id, m => m.Attempts == 1);

        await _sut.StopAsync(CancellationToken.None);
        await startTask;

        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == msg.Id);
        Assert.Equal((int)TelegramOutboxStatus.Pending, stored.Status);
        Assert.Null(stored.LockedUntil);
        Assert.Equal(1, stored.Attempts);
        Assert.NotNull(stored.NextAttemptAt);
        Assert.True(stored.NextAttemptAt.Value >= DateTime.UtcNow.AddSeconds(14));
    }

    [Fact]
    public async Task Process_PermanentError_MarksAsDead()
    {
        var msg = await SeedMessageAsync();

        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
            Task.FromResult<TelegramSendResult>(new TelegramSendResult.Error(new TelegramPermanentError("Chat blocked by user")));

        var startTask = _sut.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        await WaitForWorkerIterationAsync(msg.Id, m => m.Status == (int)TelegramOutboxStatus.Dead);

        await _sut.StopAsync(CancellationToken.None);
        await startTask;

        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == msg.Id);
        Assert.Equal((int)TelegramOutboxStatus.Dead, stored.Status);
        Assert.Null(stored.LockedUntil);
        Assert.Equal(1, stored.Attempts);
    }

    [Fact]
    public async Task Process_TransientError_AppliesExponentialBackoff()
    {
        var msg = await SeedMessageAsync();

        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
            Task.FromResult<TelegramSendResult>(new TelegramSendResult.Error(new TelegramTransientError("Timeout")));

        var startTask = _sut.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        await WaitForWorkerIterationAsync(msg.Id, m => m.Attempts == 1);

        await _sut.StopAsync(CancellationToken.None);
        await startTask;

        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == msg.Id);
        Assert.Equal((int)TelegramOutboxStatus.Pending, stored.Status);
        Assert.Null(stored.LockedUntil);
        Assert.Equal(1, stored.Attempts);
        Assert.NotNull(stored.NextAttemptAt);
        Assert.True(stored.NextAttemptAt.Value >= DateTime.UtcNow.AddSeconds(9));
    }

    [Fact]
    public async Task Process_MaxAttemptsExceeded_MarksAsFailed()
    {
        var msg = await SeedMessageAsync(attempts: 2);

        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
            Task.FromResult<TelegramSendResult>(new TelegramSendResult.Error(new TelegramTransientError("Network error")));

        var startTask = _sut.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        await WaitForWorkerIterationAsync(msg.Id, m => m.Status == (int)TelegramOutboxStatus.Failed);

        await _sut.StopAsync(CancellationToken.None);
        await startTask;

        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == msg.Id);
        Assert.Equal((int)TelegramOutboxStatus.Failed, stored.Status);
        Assert.Null(stored.LockedUntil);
        Assert.Equal(3, stored.Attempts);
    }

    private async Task WaitForWorkerIterationAsync(Guid messageId, Func<TelegramOutboxMessage, bool> predicate)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!cts.IsCancellationRequested)
        {
            await using var db = CreateNewDbContext();
            var msg = await db.TelegramOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId);
            if (msg != null && predicate(msg))
            {
                return;
            }
            await Task.Delay(50, cts.Token);
        }
        throw new TimeoutException("Timed out waiting for worker to process the message.");
    }

    private NidoDbContext CreateNewDbContext()
    {
        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new NidoDbContext(options);
    }

    private async Task<TelegramOutboxMessage> SeedMessageAsync(int attempts = 0)
    {
        var hogarId = Guid.NewGuid();
        _db.Hogares.Add(new Hogare
        {
            Id = hogarId,
            Nombre = "Worker Hogar",
            CreatedAt = DateTime.UtcNow
        });

        var msg = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            ChatId = 9876543210L,
            MessageType = "alert",
            PayloadJson = "{\"text\":\"Test text\"}",
            Status = (int)TelegramOutboxStatus.Pending,
            Attempts = attempts,
            NextAttemptAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow
        };

        _db.TelegramOutboxMessages.Add(msg);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return msg;
    }

    private sealed class FakeTelegramClient : ITelegramClient
    {
        public Func<long, string, string?, TelegramInlineKeyboardMarkup?, CancellationToken, Task<TelegramSendResult>> SendHandler { get; set; }
            = (_, _, _, _, _) => Task.FromResult<TelegramSendResult>(new TelegramSendResult.Success(new TelegramMessageSent(1L)));

        public Task<TelegramSendResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode = null,
            TelegramInlineKeyboardMarkup? replyMarkup = null,
            CancellationToken ct = default)
        {
            return SendHandler(chatId, text, parseMode, replyMarkup, ct);
        }
    }

    private sealed class FakeTelegramOutboxWakeupService : ITelegramOutboxWakeupService
    {
        private readonly SemaphoreSlim _semaphore = new(0, 1);

        public void TriggerWakeup()
        {
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException) { }
        }

        public async Task WaitForMessageAsync(CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
        }
    }
}
