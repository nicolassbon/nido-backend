using Microsoft.Extensions.Caching.Memory;
using Nido.Application.Telegram;
using Nido.Infrastructure.Telegram.Pairing;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramPairingAdaptersTests
{
    [Fact]
    public void TokenHasher_ReturnsStableSha256LowerHex()
    {
        var sut = new TelegramPairingTokenHasher();

        var first = sut.Hash("abc123");
        var second = sut.Hash("abc123");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public async Task RateLimiter_BlocksOnceWindowLimitIsReached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var usuarioId = Guid.Empty;
        var sut = new TelegramPairingRateLimiter(
            cache,
            new TelegramOptions
            {
                PairingRateLimitGeneratePerWindow = 2,
                PairingRateLimitConsumePerWindow = 1,
                PairingRateLimitWindowSeconds = 60
            });

        Assert.True(await sut.TryAcquireGenerateAsync(usuarioId, CancellationToken.None));
        Assert.True(await sut.TryAcquireGenerateAsync(usuarioId, CancellationToken.None));
        Assert.False(await sut.TryAcquireGenerateAsync(usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task RateLimiter_WhenMultipleInstancesRace_DoesNotExceedWindowLimit()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new TelegramOptions
        {
            PairingRateLimitGeneratePerWindow = 3,
            PairingRateLimitConsumePerWindow = 1,
            PairingRateLimitWindowSeconds = 60
        };
        var usuarioId = Guid.NewGuid();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 64)
            .Select(_ => new TelegramPairingRateLimiter(cache, options))
            .Select(async limiter =>
            {
                await start.Task;
                return await limiter.TryAcquireGenerateAsync(usuarioId, CancellationToken.None);
            })
            .ToArray();

        start.SetResult();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(3, results.Count(static result => result));
        Assert.Equal(61, results.Count(static result => !result));
    }

    [Fact]
    public async Task RateLimiter_CodeValidate_BlocksOnceWindowLimitIsReached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        const long chatId = 123;
        var sut = new TelegramPairingRateLimiter(
            cache,
            new TelegramOptions
            {
                PairingCodeRateLimitValidatePerWindow = 2,
                PairingCodeRateLimitWindowSeconds = 60
            });

        Assert.True(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));
        Assert.True(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));
        Assert.False(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));
    }

    [Fact]
    public async Task RateLimiter_CodeValidate_UsesDedicatedWindowSeconds()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(start);
        const long chatId = 123;
        var sut = new TelegramPairingRateLimiter(
            cache,
            new TelegramOptions
            {
                PairingCodeRateLimitValidatePerWindow = 1,
                PairingCodeRateLimitWindowSeconds = 1,
                PairingRateLimitWindowSeconds = 3600
            },
            fakeTime);

        Assert.True(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));
        Assert.False(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));

        fakeTime.Advance(TimeSpan.FromSeconds(1.1));

        Assert.True(await sut.TryAcquireCodeValidateAsync(chatId, CancellationToken.None));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset start)
        {
            _now = start;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
