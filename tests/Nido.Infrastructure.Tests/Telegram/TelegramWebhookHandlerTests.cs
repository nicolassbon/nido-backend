using Nido.Application.Telegram.Idempotency;
using Nido.Application.Telegram.Webhook;
using Nido.Infrastructure.Telegram.Webhook;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_NewUpdateId_ReturnsAcceptedAndRecordsRow()
    {
        var (handler, idempotency) = BuildHandler();

        var result = await handler.HandleAsync(
            new TelegramWebhookRequest(1L, null),
            _ => Task.CompletedTask,
            CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(result);
        Assert.True(await idempotency.IsAlreadyProcessedAsync(1L, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateUpdateId_ReturnsDuplicateAndDoesNotThrow()
    {
        var (handler, idempotency) = BuildHandler();

        await handler.HandleAsync(new TelegramWebhookRequest(7L, null), _ => Task.CompletedTask, CancellationToken.None);
        var second = await handler.HandleAsync(new TelegramWebhookRequest(7L, null), _ => Task.CompletedTask, CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Duplicate>(second);
        // Idempotent: only one row was written for this update_id.
        Assert.True(await idempotency.IsAlreadyProcessedAsync(7L, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ConcurrentSameUpdate_OnlyDispatchesOnce()
    {
        var idempotency = new InMemoryIdempotencyService();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlers = Enumerable.Range(0, 16)
            .Select(_ => new TelegramWebhookHandler(idempotency))
            .ToArray();
        var dispatchCount = 0;

        var tasks = handlers
            .Select(handler => Task.Run(async () =>
            {
                await start.Task;

                return await handler.HandleAsync(
                    new TelegramWebhookRequest(100L, null),
                    async _ =>
                    {
                        Interlocked.Increment(ref dispatchCount);
                        await Task.Delay(50);
                    },
                    CancellationToken.None);
            }))
            .ToArray();

        start.SetResult();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, dispatchCount);
        Assert.Equal(1, results.Count(static result => result is TelegramWebhookResult.Accepted));
        Assert.Equal(15, results.Count(static result => result is TelegramWebhookResult.Duplicate));
        Assert.True(await idempotency.IsAlreadyProcessedAsync(100L, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullMessage_DoesNotThrow_AndReturnsAccepted()
    {
        var (handler, _) = BuildHandler();

        var result = await handler.HandleAsync(new TelegramWebhookRequest(42L, null), _ => Task.CompletedTask, CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(result);
    }

    [Fact]
    public async Task HandleAsync_WhenDispatchFails_DoesNotRecordUpdate_AndRetryIsNotSkippedAsDuplicate()
    {
        var (handler, idempotency) = BuildHandler();
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new TelegramWebhookRequest(500L, null),
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("dispatch failed");
                },
                CancellationToken.None));

        Assert.False(await idempotency.IsAlreadyProcessedAsync(500L, CancellationToken.None));

        var retry = await handler.HandleAsync(
            new TelegramWebhookRequest(500L, null),
            _ =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(retry);
        Assert.Equal(2, attempts);
        Assert.True(await idempotency.IsAlreadyProcessedAsync(500L, CancellationToken.None));
    }

    private static (TelegramWebhookHandler, ITelegramUpdateIdempotencyService) BuildHandler()
    {
        var idempotency = new InMemoryIdempotencyService();
        var handler = new TelegramWebhookHandler(idempotency);
        return (handler, idempotency);
    }

    private sealed class InMemoryIdempotencyService : ITelegramUpdateIdempotencyService
    {
        private readonly HashSet<long> _seen = new();
        private readonly object _sync = new();

        public Task<bool> IsAlreadyProcessedAsync(long updateId, CancellationToken ct)
        {
            lock (_sync)
            {
                return Task.FromResult(_seen.Contains(updateId));
            }
        }

        public Task<bool> TryReserveAsync(long updateId, string? updateHash, CancellationToken ct)
        {
            lock (_sync)
            {
                return Task.FromResult(_seen.Add(updateId));
            }
        }

        public Task ReleaseReservationAsync(long updateId, CancellationToken ct)
        {
            lock (_sync)
            {
                _seen.Remove(updateId);
                return Task.CompletedTask;
            }
        }
    }
}
