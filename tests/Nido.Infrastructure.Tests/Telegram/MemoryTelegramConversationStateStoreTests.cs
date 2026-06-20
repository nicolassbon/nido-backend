using Microsoft.Extensions.Caching.Memory;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Conversation;
using Nido.Infrastructure.Telegram.Conversation;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class MemoryTelegramConversationStateStoreTests
{
    [Fact]
    public async Task GetAsync_WhenStateWasSet_ReturnsStoredState()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new MemoryTelegramConversationStateStore(cache, new TelegramOptions(), timeProvider);
        var state = new TelegramConversationState(321, "main-menu", timeProvider.GetUtcNow().UtcDateTime, null);

        await store.SetAsync(state, CancellationToken.None);

        var result = await store.GetAsync(321, CancellationToken.None);

        Assert.Equal(state, result);
    }

    [Fact]
    public async Task ClearAsync_RemovesStoredState()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryTelegramConversationStateStore(cache, new TelegramOptions(), new FakeTimeProvider(DateTimeOffset.UtcNow));
        await store.SetAsync(new TelegramConversationState(321, "main-menu", DateTime.UtcNow, null), CancellationToken.None);

        await store.ClearAsync(321, CancellationToken.None);

        var result = await store.GetAsync(321, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenStateExpired_ClearsAndReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new MemoryTelegramConversationStateStore(
            cache,
            new TelegramOptions { ConversationStateTtlMinutes = 1 },
            timeProvider);

        await store.SetAsync(new TelegramConversationState(321, "main-menu", timeProvider.GetUtcNow().UtcDateTime, null), CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        var result = await store.GetAsync(321, CancellationToken.None);

        Assert.Null(result);
        Assert.False(cache.TryGetValue("telegram:conversation:321", out _));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
