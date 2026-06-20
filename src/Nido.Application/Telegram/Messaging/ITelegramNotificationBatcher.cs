using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Messaging;

public interface ITelegramNotificationBatcher
{
    Task EnqueueEventAsync(
        Guid hogarId,
        long chatId,
        string messageType,
        string payloadJson,
        bool isCritical,
        CancellationToken ct = default);

    Task ProcessExpiredBatchesAsync(CancellationToken ct = default);
}
