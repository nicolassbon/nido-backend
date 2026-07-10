using Nido.Application.Payments;

namespace Nido.Application.Tests.Payments;

public sealed class CreateCheckoutPreferenceHandlerTests
{
    [Fact]
    public async Task Handle_FreeHousehold_ReturnsPreferenceIdAndInitPoint()
    {
        var hogarId = Guid.NewGuid();
        var gateway = new FakeMercadoPagoGateway();
        var handler = new CreateCheckoutPreferenceHandler(gateway);

        var result = await handler.Handle(new CreateCheckoutPreferenceCommand(hogarId), CancellationToken.None);

        Assert.Equal("pref-123", result.PreferenceId);
        Assert.Equal("https://mp.test/checkout", result.InitPoint);
        Assert.Equal(hogarId, gateway.LastRequest!.HogarId);
    }

    private sealed class FakeMercadoPagoGateway : IMercadoPagoGateway
    {
        public MercadoPagoCheckoutPreferenceRequest? LastRequest { get; private set; }

        public Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(MercadoPagoCheckoutPreferenceRequest request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new MercadoPagoCheckoutPreference("pref-123", new Uri("https://mp.test/checkout")));
        }

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
