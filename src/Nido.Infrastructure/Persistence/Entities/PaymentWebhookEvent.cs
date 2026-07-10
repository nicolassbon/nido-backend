namespace Nido.Infrastructure.Persistence.Entities;

public sealed class PaymentWebhookEvent
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderEventId { get; set; } = null!;

    public string? ProviderPaymentId { get; set; }

    public string? ProviderSubscriptionId { get; set; }

    public string EventType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public Guid HogarId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
