using Nido.Application.Payments;
using Nido.Application.Payments.Exceptions;

namespace Nido.Infrastructure.Payments;

public sealed class DisabledMercadoPagoGateway : IMercadoPagoGateway
{
    public Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(
        MercadoPagoCheckoutPreferenceRequest request,
        CancellationToken ct)
        => throw new MercadoPagoDisabledException();

    public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct)
        => throw new MercadoPagoDisabledException();
}
