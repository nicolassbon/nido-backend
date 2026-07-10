namespace Nido.Application.Payments;

public interface IPaymentRepository
{
    Task<HouseholdEntitlement> GetSubscriptionAsync(Guid hogarId, CancellationToken ct);

    Task<ProcessWebhookOutcome> ProcessWebhookEventAsync(PaymentWebhookEventRecord webhookEvent, PaymentPlanUpdate planUpdate, CancellationToken ct);
}

public sealed record PaymentWebhookEventRecord(
    string Provider,
    string ProviderEventId,
    string? ProviderPaymentId,
    string? ProviderSubscriptionId,
    string EventType,
    string Payload,
    Guid HogarId);

public sealed record PaymentPlanUpdate(
    Guid HogarId,
    HouseholdPlan Plan,
    SubscriptionStatus SubscriptionStatus,
    string? ProviderPaymentId,
    string? ProviderSubscriptionId,
    DateTime? SubscriptionEndsAt,
    DateTime ProviderTransitionAt);
