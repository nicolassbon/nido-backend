using Nido.Application.Payments;

namespace Nido.Application.Tests.Payments;

public sealed class MercadoPagoOptionsTests
{
    [Theory]
    [InlineData("https://nidoapp.online")]
    [InlineData("https://www.nidoapp.online")]
    [InlineData("https://frontend.example.com/app")]
    public void HasApprovedProductionBaseUrl_WithExternalHttpsUrl_ReturnsTrue(string baseUrl)
    {
        Assert.True(FrontendOptions.HasApprovedProductionBaseUrl(new FrontendOptions
        {
            BaseUrl = baseUrl
        }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("http://nidoapp.online")]
    [InlineData("https://localhost:4200")]
    [InlineData("https://user:password@nidoapp.online")]
    public void HasApprovedProductionBaseUrl_WithUnsafeOrMissingUrl_ReturnsFalse(string baseUrl)
    {
        Assert.False(FrontendOptions.HasApprovedProductionBaseUrl(new FrontendOptions
        {
            BaseUrl = baseUrl
        }));
    }

    [Theory]
    [InlineData("https://api.mercadopago.com")]
    [InlineData("https://api.mercadopago.com/")]
    public void HasApprovedProductionApiBaseUrl_WithApprovedHttpsHost_ReturnsTrue(string apiBaseUrl)
    {
        var result = MercadoPagoOptions.HasApprovedProductionApiBaseUrl(new MercadoPagoOptions { ApiBaseUrl = apiBaseUrl });

        Assert.True(result);
    }

    [Theory]
    [InlineData("http://api.mercadopago.com")]
    [InlineData("https://api.mercadopago.com.attacker.test")]
    [InlineData("https://api.mercadopago.test")]
    [InlineData("not-a-url")]
    public void HasApprovedProductionApiBaseUrl_WithUnapprovedUrl_ReturnsFalse(string apiBaseUrl)
    {
        var result = MercadoPagoOptions.HasApprovedProductionApiBaseUrl(new MercadoPagoOptions { ApiBaseUrl = apiBaseUrl });

        Assert.False(result);
    }

    [Theory]
    [InlineData(MercadoPagoMode.Sandbox)]
    [InlineData(MercadoPagoMode.Production)]
    public void HasValidMode_WithSupportedMode_ReturnsTrue(MercadoPagoMode mode)
    {
        Assert.True(MercadoPagoOptions.HasValidMode(new MercadoPagoOptions { Mode = mode }));
    }

    [Fact]
    public void HasValidMode_WithoutExplicitMode_ReturnsFalse()
    {
        Assert.False(MercadoPagoOptions.HasValidMode(new MercadoPagoOptions()));
    }
}
