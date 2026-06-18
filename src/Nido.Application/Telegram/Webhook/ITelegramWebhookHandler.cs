using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Webhook;

public interface ITelegramWebhookHandler
{
    Task<TelegramWebhookResult> HandleAsync(TelegramWebhookRequest request, CancellationToken ct);
}
