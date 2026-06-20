using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Messaging;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramOutboxWriterTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private FakeTelegramOutboxWakeupService _wakeupService = null!;
    private TelegramOutboxWriter _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_outbox_writer");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();

        _wakeupService = new FakeTelegramOutboxWakeupService();
        _sut = new TelegramOutboxWriter(_db, _wakeupService);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueAsync_SavesMessageToDatabaseWithPendingStatus()
    {
        var hogarId = Guid.NewGuid();
        var chatId = 123456789L;
        var messageType = "test_alert";
        var payloadJson = "{\"value\":\"test\"}";
        var scheduledFor = DateTime.UtcNow.AddMinutes(5);

        SeedUserAndHousehold(hogarId);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var request = new EnqueueTelegramMessageRequest(
            hogarId,
            chatId,
            messageType,
            payloadJson,
            scheduledFor);

        var result = await _sut.EnqueueAsync(request, CancellationToken.None);

        // Assert response result
        Assert.NotEqual(Guid.Empty, result.MessageId);
        Assert.Equal(chatId, result.ChatId);
        Assert.Equal(messageType, result.MessageType);
        Assert.Equal(payloadJson, result.PayloadJson);
        Assert.Equal(TelegramOutboxStatus.Pending, result.Status);
        Assert.Equal(0, result.Attempts);
        Assert.Equal(scheduledFor, result.NextAttemptAt);
        Assert.True(_wakeupService.Triggered);

        // Assert database record
        var stored = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == result.MessageId);
        Assert.Equal(hogarId, stored.HogarId);
        Assert.Equal(chatId, stored.ChatId);
        Assert.Equal(messageType, stored.MessageType);
        Assert.Equal(payloadJson, stored.PayloadJson);
        Assert.Equal((int)TelegramOutboxStatus.Pending, stored.Status);
        Assert.Equal(0, stored.Attempts);
        Assert.Equal(scheduledFor, stored.NextAttemptAt);
        Assert.Null(stored.BatchId);
        Assert.Null(stored.LockedUntil);
    }

    private void SeedUserAndHousehold(Guid hogarId)
    {
        _db.Hogares.Add(new Hogare
        {
            Id = hogarId,
            Nombre = "Test Hogar",
            CreatedAt = DateTime.UtcNow
        });
    }

    private sealed class FakeTelegramOutboxWakeupService : ITelegramOutboxWakeupService
    {
        public bool Triggered { get; private set; }

        public void TriggerWakeup()
        {
            Triggered = true;
        }

        public Task WaitForMessageAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
