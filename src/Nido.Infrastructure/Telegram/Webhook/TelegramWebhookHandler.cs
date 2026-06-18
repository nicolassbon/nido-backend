using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Telegram.Idempotency;
using Nido.Application.Telegram.Webhook;

namespace Nido.Infrastructure.Telegram.Webhook;

public sealed class TelegramWebhookHandler : ITelegramWebhookHandler
{
    private readonly ITelegramUpdateIdempotencyService _idempotency;

    public TelegramWebhookHandler(ITelegramUpdateIdempotencyService idempotency)
    {
        _idempotency = idempotency;
    }

    public async Task<TelegramWebhookResult> HandleAsync(TelegramWebhookRequest request, CancellationToken ct)
    {
        var alreadyProcessed = await _idempotency.IsAlreadyProcessedAsync(request.UpdateId, ct);
        if (alreadyProcessed)
        {
            return new TelegramWebhookResult.Duplicate();
        }

        var recorded = await _idempotency.RecordProcessedAsync(request.UpdateId, updateHash: null, ct);
        if (!recorded)
        {
            return new TelegramWebhookResult.Duplicate();
        }

        return new TelegramWebhookResult.Accepted();
    }
}
