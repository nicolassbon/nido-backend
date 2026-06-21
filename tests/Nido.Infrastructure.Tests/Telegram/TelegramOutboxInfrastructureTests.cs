using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Messaging;
using Nido.Tests.Shared;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramOutboxInfrastructureTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private TelegramOutboxWriter _writer = null!;
    private TelegramOutboxReader _reader = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_outbox_infra");
        _db = CreateDbContext();
        await _db.Database.MigrateAsync();
        var optionsVal = new TelegramOptions { BotToken = "test_token" };
        _writer = new TelegramOutboxWriter(_db, new FakeTelegramOutboxWakeupService(), optionsVal, NullLogger<TelegramOutboxWriter>.Instance);
        _reader = new TelegramOutboxReader(_db, new TelegramOptions());
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueAsync_PersistsPendingInteractiveMessage()
    {
        var result = await _writer.EnqueueAsync(
            new EnqueueTelegramMessageRequest(Guid.NewGuid(), 301, "interactive.menu", JsonSerializer.Serialize(new { text = "hello", parseMode = "MarkdownV2" })),
            CancellationToken.None);

        Assert.Equal(TelegramOutboxStatus.Pending, result.Status);

        var row = await _db.TelegramOutboxMessages.SingleAsync(x => x.Id == result.MessageId);
        Assert.Equal(301, row.ChatId);
        Assert.Equal("interactive.menu", row.MessageType);
        Assert.Equal((int)TelegramOutboxStatus.Pending, row.Status);
    }

    [Fact]
    public async Task EnqueueAsync_WhenPendingDuplicateExists_ReturnsExistingRow()
    {
        var hogarId = Guid.NewGuid();
        var request = new EnqueueTelegramMessageRequest(hogarId, 302, "interactive.menu", JsonSerializer.Serialize(new { text = "hello" }));

        var first = await _writer.EnqueueAsync(request, CancellationToken.None);
        var second = await _writer.EnqueueAsync(request, CancellationToken.None);

        Assert.Equal(first.MessageId, second.MessageId);
        Assert.Equal(1, await _db.TelegramOutboxMessages.CountAsync());
    }

    [Fact]
    public async Task DequeuePendingAsync_ReturnsOnlyUnlockedPendingRows_AndLocksThem()
    {
        var expected1 = await SeedMessageAsync("interactive.menu", TelegramOutboxStatus.Pending, nextAttemptAt: DateTime.UtcNow.AddMinutes(-1));
        var expected2 = await SeedMessageAsync("digest.daily", TelegramOutboxStatus.Pending, nextAttemptAt: DateTime.UtcNow.AddMinutes(-1));
        await SeedMessageAsync("interactive.retry", TelegramOutboxStatus.Pending, nextAttemptAt: DateTime.UtcNow.AddMinutes(2));
        await SeedMessageAsync("interactive.locked", TelegramOutboxStatus.Pending, lockedUntil: DateTime.UtcNow.AddMinutes(2));

        var leases = await _reader.DequeuePendingAsync(10, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, leases.Count);
        Assert.Contains(leases, l => l.MessageId == expected1.Id);
        Assert.Contains(leases, l => l.MessageId == expected2.Id);

        await using var verifyDb = CreateDbContext();
        var lockedRow1 = await verifyDb.TelegramOutboxMessages.AsNoTracking().SingleAsync(x => x.Id == expected1.Id);
        Assert.NotNull(lockedRow1.LockedUntil);
        Assert.True(lockedRow1.LockedUntil > DateTime.UtcNow);

        var lockedRow2 = await verifyDb.TelegramOutboxMessages.AsNoTracking().SingleAsync(x => x.Id == expected2.Id);
        Assert.NotNull(lockedRow2.LockedUntil);
        Assert.True(lockedRow2.LockedUntil > DateTime.UtcNow);
    }

    private async Task<TelegramOutboxMessage> SeedMessageAsync(
        string messageType,
        TelegramOutboxStatus status,
        DateTime? nextAttemptAt = null,
        DateTime? lockedUntil = null)
    {
        var row = new TelegramOutboxMessage
        {
            Id = Guid.NewGuid(),
            HogarId = Guid.NewGuid(),
            ChatId = Random.Shared.NextInt64(1, 10_000),
            MessageType = messageType,
            PayloadJson = JsonSerializer.Serialize(new { text = messageType }),
            Status = (int)status,
            NextAttemptAt = nextAttemptAt,
            LockedUntil = lockedUntil,
            CreatedAt = DateTime.UtcNow
        };

        _db.TelegramOutboxMessages.Add(row);
        await _db.SaveChangesAsync();
        return row;
    }

    private NidoDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    private sealed class FakeTelegramOutboxWakeupService : ITelegramOutboxWakeupService
    {
        public void TriggerWakeup() { }
        public Task WaitForMessageAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
