namespace Nido.Application.Telegram.Messaging;

public interface ITelegramOutboxWriter
{
    Task<TelegramMessageResult> EnqueueAsync(EnqueueTelegramMessageRequest request, CancellationToken ct);
}
