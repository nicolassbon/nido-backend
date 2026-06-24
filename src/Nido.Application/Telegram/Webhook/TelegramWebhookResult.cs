namespace Nido.Application.Telegram.Webhook;

public abstract record TelegramWebhookResult
{
    public sealed record Accepted : TelegramWebhookResult;

    public sealed record Duplicate : TelegramWebhookResult;

    public sealed record Rejected(string Reason) : TelegramWebhookResult;
}
