using System;
using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Telegram.Idempotency;
using Nido.Application.Telegram.Webhook;

namespace Nido.Infrastructure.Telegram.Webhook;

public sealed class TelegramWebhookHandler : ITelegramWebhookHandler
{
    private static readonly SemaphoreSlim[] ProcessingStripes = CreateProcessingStripes();
    private readonly ITelegramUpdateIdempotencyService _idempotency;

    public TelegramWebhookHandler(ITelegramUpdateIdempotencyService idempotency)
    {
        _idempotency = idempotency;
    }

    public async Task<TelegramWebhookResult> HandleAsync(
        TelegramWebhookRequest request,
        Func<CancellationToken, Task> dispatch,
        CancellationToken ct)
    {
        var stripe = GetProcessingStripe(request.UpdateId);
        await stripe.WaitAsync(ct);

        try
        {
            var reserved = await _idempotency.TryReserveAsync(request.UpdateId, updateHash: null, ct);
            if (!reserved)
            {
                return new TelegramWebhookResult.Duplicate();
            }

            try
            {
                await dispatch(ct);
                return new TelegramWebhookResult.Accepted();
            }
            catch
            {
                await _idempotency.ReleaseReservationAsync(request.UpdateId, ct);
                throw;
            }
        }
        finally
        {
            stripe.Release();
        }
    }

    private static SemaphoreSlim[] CreateProcessingStripes()
    {
        var stripes = new SemaphoreSlim[256];
        for (var i = 0; i < stripes.Length; i++)
        {
            stripes[i] = new SemaphoreSlim(1, 1);
        }

        return stripes;
    }

    private static SemaphoreSlim GetProcessingStripe(long updateId)
    {
        var index = (int)((ulong)updateId % (ulong)ProcessingStripes.Length);
        return ProcessingStripes[index];
    }
}
