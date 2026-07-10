namespace Nido.Application.Payments;

public interface IMercadoPagoGateway
{
    Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(MercadoPagoCheckoutPreferenceRequest request, CancellationToken ct);

    Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct);
}

public sealed record MercadoPagoCheckoutPreferenceRequest(Guid HogarId);

public sealed record MercadoPagoCheckoutPreference(string PreferenceId, Uri InitPoint);

public sealed record MercadoPagoPaymentDetails(
    string ProviderPaymentId,
    Guid HogarId,
    string Status,
    string? ProviderSubscriptionId,
    DateTime? DateApproved = null,
    DateTime? ProviderTransitionAt = null);
