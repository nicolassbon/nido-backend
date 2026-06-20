using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Messaging;

public interface ITelegramOutboxWriter
{
    Task<TelegramMessageResult> EnqueueAsync(
        EnqueueTelegramMessageRequest request,
        CancellationToken ct = default);
}
