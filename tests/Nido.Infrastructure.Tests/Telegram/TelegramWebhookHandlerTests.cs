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
            CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(result);
        Assert.True(await idempotency.IsAlreadyProcessedAsync(1L, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateUpdateId_ReturnsDuplicateAndDoesNotThrow()
    {
        var (handler, idempotency) = BuildHandler();

        await handler.HandleAsync(new TelegramWebhookRequest(7L, null), CancellationToken.None);
        var second = await handler.HandleAsync(new TelegramWebhookRequest(7L, null), CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Duplicate>(second);
        // Idempotent: only one row was written for this update_id.
        Assert.True(await idempotency.IsAlreadyProcessedAsync(7L, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateRace_ReturnsDuplicate_AndDoesNotInsertSecondRow()
    {
        var idempotency = new RaceLosingIdempotencyService();
        var handler = new TelegramWebhookHandler(idempotency);

        var first = await handler.HandleAsync(new TelegramWebhookRequest(100L, null), CancellationToken.None);
        var second = await handler.HandleAsync(new TelegramWebhookRequest(100L, null), CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(first);
        Assert.IsType<TelegramWebhookResult.Duplicate>(second);
        Assert.Equal(1, idempotency.InsertCount);
    }

    [Fact]
    public async Task HandleAsync_NullMessage_DoesNotThrow_AndReturnsAccepted()
    {
        var (handler, _) = BuildHandler();

        var result = await handler.HandleAsync(new TelegramWebhookRequest(42L, null), CancellationToken.None);

        Assert.IsType<TelegramWebhookResult.Accepted>(result);
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

        public Task<bool> IsAlreadyProcessedAsync(long updateId, CancellationToken ct)
            => Task.FromResult(_seen.Contains(updateId));

        public Task<bool> RecordProcessedAsync(long updateId, string? updateHash, CancellationToken ct)
        {
            return Task.FromResult(_seen.Add(updateId));
        }
    }

    private sealed class RaceLosingIdempotencyService : ITelegramUpdateIdempotencyService
    {
        public int InsertCount;

        public Task<bool> IsAlreadyProcessedAsync(long updateId, CancellationToken ct)
            => Task.FromResult(InsertCount > 0);

        public Task<bool> RecordProcessedAsync(long updateId, string? updateHash, CancellationToken ct)
        {
            return Task.FromResult(System.Threading.Interlocked.Exchange(ref InsertCount, 1) == 0);
        }
    }
}
