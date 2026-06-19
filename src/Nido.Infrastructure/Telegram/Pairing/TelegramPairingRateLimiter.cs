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
        => Task.FromResult(TryAcquire($"telegram:pairing:generate:{usuarioId}", options.PairingRateLimitGeneratePerWindow));

    public Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct)
        => Task.FromResult(TryAcquire($"telegram:pairing:consume:{chatId}", options.PairingRateLimitConsumePerWindow));

    private bool TryAcquire(string keyPrefix, int limit)
    {
        var now = _timeProvider.GetUtcNow();
        var windowSeconds = options.PairingRateLimitWindowSeconds;
        var window = now.ToUnixTimeSeconds() / windowSeconds;
        var key = $"{keyPrefix}:{window}";

        lock (GetGateStripe(key))
        {
            var count = memoryCache.Get<int?>(key) ?? 0;
            if (count >= limit)
            {
                return false;
            }

            memoryCache.Set(key, count + 1, TimeSpan.FromSeconds(windowSeconds + 1));
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
