using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Conversation;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Conversation;
using Nido.Tests.Shared;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class PostgresTelegramConversationStateStoreTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private FakeTimeProvider _timeProvider = null!;
    private PostgresTelegramConversationStateStore _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_conversation_state_store");
        _db = CreateDbContext();
        await _db.Database.MigrateAsync();

        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _sut = new PostgresTelegramConversationStateStore(_db, new TelegramOptions { ConversationStateTtlMinutes = 10 }, _timeProvider);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredState()
    {
        var state = new TelegramConversationState(321, "main-menu", _timeProvider.GetUtcNow().UtcDateTime, "{\"step\":1}");

        await _sut.SetAsync(state, CancellationToken.None);

        var result = await _sut.GetAsync(321, CancellationToken.None);

        Assert.Equal(state.ChatId, result!.ChatId);
        Assert.Equal(state.MenuId, result.MenuId);
        Assert.Equal(state.PayloadJson, result.PayloadJson);
    }

    [Fact]
    public async Task ClearAsync_RemovesStoredState()
    {
        await _sut.SetAsync(new TelegramConversationState(654, "main-menu", _timeProvider.GetUtcNow().UtcDateTime, null), CancellationToken.None);

        await _sut.ClearAsync(654, CancellationToken.None);

        var result = await _sut.GetAsync(654, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenStateExpired_DeletesRowAndReturnsNull()
    {
        await _sut.SetAsync(new TelegramConversationState(777, "main-menu", _timeProvider.GetUtcNow().UtcDateTime, null), CancellationToken.None);
        _timeProvider.Advance(TimeSpan.FromMinutes(11));

        var result = await _sut.GetAsync(777, CancellationToken.None);

        Assert.Null(result);
        Assert.False(await _db.Set<TelegramConversationStateEntity>().AnyAsync(x => x.ChatId == 777));
    }

    [Fact]
    public async Task State_IsVisibleAcrossStoreInstances()
    {
        await _sut.SetAsync(new TelegramConversationState(888, "main-menu", _timeProvider.GetUtcNow().UtcDateTime, "{\"source\":\"a\"}"), CancellationToken.None);

        await using var secondDb = CreateDbContext();
        var secondStore = new PostgresTelegramConversationStateStore(secondDb, new TelegramOptions { ConversationStateTtlMinutes = 10 }, _timeProvider);

        var result = await secondStore.GetAsync(888, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("main-menu", result!.MenuId);
        Assert.Equal("{\"source\":\"a\"}", result.PayloadJson);
    }

    private NidoDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
