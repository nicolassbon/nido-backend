using Nido.Application.Payments;

namespace Nido.Application.Tests.Payments;

public sealed class MercadoPagoOptionsTests
{
    [Fact]
    public void HasApprovedProductionBaseUrl_WithCanonicalHost_ReturnsTrue()
    {
        Assert.True(FrontendOptions.HasApprovedProductionBaseUrl(new FrontendOptions
        {
            BaseUrl = "https://nidoapp.online"
        }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("http://nidoapp.online")]
    [InlineData("https://localhost:4200")]
    [InlineData("https://nidoapp.online.attacker.test")]
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
    [InlineData(MercadoPagoMode.Disabled)]
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

    [Theory]
    [InlineData(MercadoPagoMode.Sandbox, true)]
    [InlineData(MercadoPagoMode.Production, true)]
    [InlineData(MercadoPagoMode.Disabled, false)]
    public void HasEnabledMode_WithConfiguredMode_ReturnsExpectedResult(MercadoPagoMode mode, bool expected)
    {
        Assert.Equal(expected, MercadoPagoOptions.HasEnabledMode(new MercadoPagoOptions { Mode = mode }));
    }

    [Fact]
    public void HasEnabledMode_WithoutExplicitMode_ReturnsFalse()
    {
        Assert.False(MercadoPagoOptions.HasEnabledMode(new MercadoPagoOptions()));
    }

    [Fact]
    public void HasRequiredEnabledConfiguration_WhenDisabledWithoutCredentials_ReturnsTrue()
    {
        Assert.True(MercadoPagoOptions.HasRequiredEnabledConfiguration(new MercadoPagoOptions
        {
            Mode = MercadoPagoMode.Disabled
        }));
    }

    [Theory]
    [InlineData(MercadoPagoMode.Sandbox)]
    [InlineData(MercadoPagoMode.Production)]
    public void HasRequiredEnabledConfiguration_WhenEnabledWithCredentials_ReturnsTrue(MercadoPagoMode mode)
    {
        Assert.True(MercadoPagoOptions.HasRequiredEnabledConfiguration(new MercadoPagoOptions
        {
            Mode = mode,
            AccessToken = "intentional-access-token",
            WebhookSecret = "intentional-webhook-secret",
            UnitPrice = 1m
        }));
    }

    [Theory]
    [InlineData(MercadoPagoMode.Sandbox, "", "secret", 1)]
    [InlineData(MercadoPagoMode.Sandbox, "token", "", 1)]
    [InlineData(MercadoPagoMode.Sandbox, "token", "secret", 0)]
    [InlineData(MercadoPagoMode.Production, "", "secret", 1)]
    public void HasRequiredEnabledConfiguration_WhenEnabledConfigurationIsIncomplete_ReturnsFalse(
        MercadoPagoMode mode,
        string accessToken,
        string webhookSecret,
        decimal unitPrice)
    {
        Assert.False(MercadoPagoOptions.HasRequiredEnabledConfiguration(new MercadoPagoOptions
        {
            Mode = mode,
            AccessToken = accessToken,
            WebhookSecret = webhookSecret,
            UnitPrice = unitPrice
        }));
    }
}
