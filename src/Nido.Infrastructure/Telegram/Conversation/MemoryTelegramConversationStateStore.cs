using Microsoft.Extensions.Caching.Memory;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Conversation;

namespace Nido.Infrastructure.Telegram.Conversation;

public sealed class MemoryTelegramConversationStateStore(
    IMemoryCache cache,
    TelegramOptions options,
    TimeProvider? timeProvider = null) : ITelegramConversationStateStore
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct)
    {
        if (!cache.TryGetValue(GetCacheKey(chatId), out CacheEntry? entry) || entry is null)
        {
            return Task.FromResult<TelegramConversationState?>(null);
        }

        if (entry.ExpiresAtUtc <= _timeProvider.GetUtcNow())
        {
            cache.Remove(GetCacheKey(chatId));
            return Task.FromResult<TelegramConversationState?>(null);
        }

        return Task.FromResult<TelegramConversationState?>(entry.State);
    }

    public Task SetAsync(TelegramConversationState state, CancellationToken ct)
    {
        var expiresAtUtc = _timeProvider.GetUtcNow().AddMinutes(options.ConversationStateTtlMinutes);
        var entry = new CacheEntry(state, expiresAtUtc);

        cache.Set(GetCacheKey(state.ChatId), entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAtUtc
        });

        return Task.CompletedTask;
    }

    public Task ClearAsync(long chatId, CancellationToken ct)
    {
        cache.Remove(GetCacheKey(chatId));
        return Task.CompletedTask;
    }

    private static string GetCacheKey(long chatId) => $"telegram:conversation:{chatId}";

    private sealed record CacheEntry(TelegramConversationState State, DateTimeOffset ExpiresAtUtc);
}
