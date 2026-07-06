using Microsoft.Extensions.Caching.Memory;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Pairing;

namespace Nido.Infrastructure.Telegram.Pairing;

public sealed class TelegramPairingRateLimiter(
    IMemoryCache memoryCache,
    TelegramOptions options,
    TimeProvider? timeProvider = null) : ITelegramPairingRateLimiter
{
    private static readonly object[] GateStripes = CreateGateStripes();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task<bool> TryAcquireGenerateAsync(Guid usuarioId, CancellationToken ct)
        => Task.FromResult(TryAcquire(
            $"telegram:pairing:generate:{usuarioId}",
            options.PairingRateLimitGeneratePerWindow,
            options.PairingRateLimitWindowSeconds));

    public Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct)
        => Task.FromResult(TryAcquire(
            $"telegram:pairing:consume:{chatId}",
            options.PairingRateLimitConsumePerWindow,
            options.PairingRateLimitWindowSeconds));

    public Task<bool> TryAcquireCodeValidateAsync(long chatId, CancellationToken ct)
        => Task.FromResult(TryAcquire(
            $"telegram:pairing:code-validate:{chatId}",
            options.PairingCodeRateLimitValidatePerWindow,
            options.PairingCodeRateLimitWindowSeconds));

    private bool TryAcquire(string key, int limit, int windowSeconds)
    {
        var now = _timeProvider.GetUtcNow();
        var windowStart = now - TimeSpan.FromSeconds(windowSeconds);

        lock (GetGateStripe(key))
        {
            var timestamps = memoryCache.Get<List<DateTimeOffset>>(key) ?? [];
            timestamps.RemoveAll(timestamp => timestamp <= windowStart);

            if (timestamps.Count >= limit)
            {
                memoryCache.Set(key, timestamps, TimeSpan.FromSeconds(windowSeconds + 1));
                return false;
            }

            timestamps.Add(now);
            memoryCache.Set(key, timestamps, TimeSpan.FromSeconds(windowSeconds + 1));
            return true;
        }
    }

    private static object[] CreateGateStripes()
    {
        var stripes = new object[256];
        for (var i = 0; i < stripes.Length; i++)
        {
            stripes[i] = new object();
        }

        return stripes;
    }

    private static object GetGateStripe(string key)
    {
        var index = (key.GetHashCode() & int.MaxValue) % GateStripes.Length;
        return GateStripes[index];
    }
}
