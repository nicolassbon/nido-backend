using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

public sealed class TelegramNotificationBatcherTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private ServiceProvider _serviceProvider = null!;
    private NidoDbContext _db = null!;
    private FakeTelegramClient _fakeTelegramClient = null!;
    private FakeTelegramOutboxWakeupService _wakeupService = null!;
    private TelegramOptions _options = null!;
    private TelegramNotificationBatcher _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_batcher_tests");

        var services = new ServiceCollection();

        services.AddDbContext<NidoDbContext>(options =>
            options.UseNpgsql(_database.ConnectionString)
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        _fakeTelegramClient = new FakeTelegramClient();
        _wakeupService = new FakeTelegramOutboxWakeupService();
        _options = new TelegramOptions
        {
            BotToken = "test_token",
            GroupingEarlySendThreshold = 3,
            GroupingWindowMinutes = 10,
            OutboxMaxBatchSize = 10,
            MaxAttempts = 3,
            DefaultParseMode = "MarkdownV2"
        };

        services.AddSingleton<ITelegramClient>(_fakeTelegramClient);
        services.AddSingleton(_options);
        services.AddSingleton<ITelegramOutboxWakeupService>(_wakeupService);
        services.AddScoped<ITelegramOutboxWriter, TelegramOutboxWriter>();
        services.AddScoped<TelegramNotificationBatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<NidoDbContext>();
        await _db.Database.MigrateAsync();

        _sut = _serviceProvider.GetRequiredService<TelegramNotificationBatcher>();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueEventAsync_CriticalMessage_EnqueuesDirectlyAsPending()
    {
        var hogarId = await SeedHogarAsync();

        await _sut.EnqueueEventAsync(
            hogarId,
            123456789L,
            "CriticalAlert",
            "{\"text\":\"Critical error!\"}",
            isCritical: true,
            CancellationToken.None);

        var messages = await _db.TelegramOutboxMessages.ToListAsync();
        Assert.Single(messages);

        var msg = messages[0];
        Assert.Equal("CriticalAlert", msg.MessageType);
        Assert.Equal("{\"text\":\"Critical error!\"}", msg.PayloadJson);
        Assert.Equal((int)TelegramOutboxStatus.Pending, msg.Status);
        Assert.Null(msg.BatchId);
        Assert.True(_wakeupService.WasWokenUp);
    }

    [Fact]
    public async Task EnqueueEventAsync_NormalMessage_GroupsUnderPendingBatch()
    {
        var hogarId = await SeedHogarAsync();

        await _sut.EnqueueEventAsync(
            hogarId,
            123456789L,
            "NormalAlert",
            "{\"text\":\"Normal message 1\"}",
            isCritical: false,
            CancellationToken.None);

        var batches = await _db.TelegramBatches.ToListAsync();
        Assert.Single(batches);
        Assert.Equal((int)TelegramBatchStatus.Pending, batches[0].Status);

        var messages = await _db.TelegramOutboxMessages.ToListAsync();
        Assert.Single(messages);

        var msg = messages[0];
        Assert.Equal("NormalAlert", msg.MessageType);
        Assert.Equal((int)TelegramOutboxStatus.Ready, msg.Status); // Status = Ready (1) so sender doesn't process it yet
        Assert.Equal(batches[0].Id, msg.BatchId);
    }

    [Fact]
    public async Task EnqueueEventAsync_MultipleNormalMessages_ClosesBatchOnThresholdReached()
    {
        var hogarId = await SeedHogarAsync();

        // GroupingEarlySendThreshold is 3
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert1", "{\"text\":\"Msg 1\"}", isCritical: false);
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert2", "{\"text\":\"Msg 2\"}", isCritical: false);

        var messagesBefore = await _db.TelegramOutboxMessages.ToListAsync();
        Assert.Equal(2, messagesBefore.Count);
        Assert.All(messagesBefore, m => Assert.Equal((int)TelegramOutboxStatus.Ready, m.Status));

        var batchBefore = await _db.TelegramBatches.SingleAsync();
        Assert.Equal((int)TelegramBatchStatus.Pending, batchBefore.Status);

        // Third message triggers early send threshold
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert3", "{\"text\":\"Msg 3\"}", isCritical: false);

        var batchAfter = await _db.TelegramBatches.SingleAsync();
        Assert.Equal((int)TelegramBatchStatus.Ready, batchAfter.Status);

        var messagesAfter = await _db.TelegramOutboxMessages.ToListAsync();
        // 3 individual messages + 1 consolidated message = 4 total messages
        Assert.Equal(4, messagesAfter.Count);

        var consolidated = messagesAfter.Single(m => m.MessageType == $"Batch_{batchAfter.Id:N}");
        Assert.Equal((int)TelegramOutboxStatus.Pending, consolidated.Status);

        using var doc = JsonDocument.Parse(consolidated.PayloadJson);
        var text = doc.RootElement.GetProperty("text").GetString();
        Assert.Equal("• Msg 1\n• Msg 2\n• Msg 3", text);
        Assert.True(_wakeupService.WasWokenUp);
    }

    [Fact]
    public async Task ProcessExpiredBatchesAsync_ExpiredBatch_ClosesAndConsolidates()
    {
        var hogarId = await SeedHogarAsync();

        // Enqueue messages but do not reach the threshold of 3
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert1", "{\"text\":\"Msg 1\"}", isCritical: false);
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert2", "{\"text\":\"Msg 2\"}", isCritical: false);

        var batch = await _db.TelegramBatches.SingleAsync();
        // Force the batch to be created in the past (e.g. 20 minutes ago)
        batch.CreatedAt = DateTime.UtcNow.AddMinutes(-20);
        await _db.SaveChangesAsync();

        _wakeupService.WasWokenUp = false;

        await _sut.ProcessExpiredBatchesAsync(CancellationToken.None);

        var updatedBatch = await _db.TelegramBatches.SingleAsync();
        Assert.Equal((int)TelegramBatchStatus.Ready, updatedBatch.Status);

        var messages = await _db.TelegramOutboxMessages.ToListAsync();
        Assert.Equal(3, messages.Count); // 2 original + 1 consolidated

        var consolidated = messages.Single(m => m.MessageType == $"Batch_{updatedBatch.Id:N}");
        Assert.Equal((int)TelegramOutboxStatus.Pending, consolidated.Status);

        using var doc = JsonDocument.Parse(consolidated.PayloadJson);
        var text = doc.RootElement.GetProperty("text").GetString();
        Assert.Equal("• Msg 1\n• Msg 2", text);
        Assert.True(_wakeupService.WasWokenUp);
    }

    [Fact]
    public async Task SenderWorker_OnSuccessfulBatchSend_CascadesSentStatusToBatchAndAllMessages()
    {
        var hogarId = await SeedHogarAsync();

        // Enqueue 3 messages to close the batch and produce the consolidated message
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert1", "{\"text\":\"Msg 1\"}", isCritical: false);
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert2", "{\"text\":\"Msg 2\"}", isCritical: false);
        await _sut.EnqueueEventAsync(hogarId, 123456789L, "Alert3", "{\"text\":\"Msg 3\"}", isCritical: false);

        var batch = await _db.TelegramBatches.SingleAsync();
        var messages = await _db.TelegramOutboxMessages.ToListAsync();
        var consolidated = messages.Single(m => m.MessageType == $"Batch_{batch.Id:N}");

        // Now run the TelegramSenderWorker to process this consolidated message
        _fakeTelegramClient.SendHandler = (chatId, text, parseMode, replyMarkup, ct) =>
        {
            Assert.Equal(123456789L, chatId);
            Assert.Equal("• Msg 1\n• Msg 2\n• Msg 3", text);
            return Task.FromResult<TelegramSendResult>(new TelegramSendResult.Success(new TelegramMessageSent(999L)));
        };

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramSenderWorker>.Instance;
        var senderWorker = new TelegramSenderWorker(_serviceProvider, _wakeupService, _options, logger);

        var startTask = senderWorker.StartAsync(CancellationToken.None);
        _wakeupService.TriggerWakeup();

        // Wait until consolidated message status updates to Sent (2)
        await WaitForMessageStatusAsync(consolidated.Id, TelegramOutboxStatus.Sent);

        await senderWorker.StopAsync(CancellationToken.None);
        await startTask;

        // Verify database statuses
        _db.ChangeTracker.Clear();
        var updatedBatch = await _db.TelegramBatches.SingleAsync();
        Assert.Equal((int)TelegramBatchStatus.Sent, updatedBatch.Status);

        var updatedMessages = await _db.TelegramOutboxMessages.ToListAsync();
        // Every message including the consolidated one and individual ones must be Sent
        Assert.All(updatedMessages, m => Assert.Equal((int)TelegramOutboxStatus.Sent, m.Status));
    }

    private async Task WaitForMessageStatusAsync(Guid id, TelegramOutboxStatus expectedStatus)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!cts.IsCancellationRequested)
        {
            await using var db = new NidoDbContext(
                new DbContextOptionsBuilder<NidoDbContext>()
                    .UseNpgsql(_database.ConnectionString)
                    .Options);

            var msg = await db.TelegramOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (msg != null && msg.Status == (int)expectedStatus)
            {
                return;
            }
            await Task.Delay(50, cts.Token);
        }
        throw new TimeoutException($"Timed out waiting for message {id} to reach status {expectedStatus}");
    }

    private async Task<Guid> SeedHogarAsync()
    {
        var id = Guid.NewGuid();
        _db.Hogares.Add(new Hogare
        {
            Id = id,
            Nombre = "Batcher Hogar",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return id;
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
        public bool WasWokenUp { get; set; }

        public void TriggerWakeup()
        {
            WasWokenUp = true;
            try
            {
                _semaphore.Release();
            }
            catch (SemaphoreFullException) { }
            catch (ObjectDisposedException) { }
        }

        public async Task WaitForMessageAsync(CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
        }
    }
}
