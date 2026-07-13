using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nido.Application.Payments;

namespace Nido.Api.IntegrationTests.Payments;

public sealed class MercadoPagoHostConfigurationTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public MercadoPagoHostConfigurationTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Disabled_OutsideProduction_StartsWithoutCredentials()
    {
        using var factory = _factory.WithConfiguration(new Dictionary<string, string?>
        {
            ["MercadoPago:Mode"] = "Disabled",
            ["MercadoPago:AccessToken"] = "",
            ["MercadoPago:WebhookSecret"] = "",
            ["MercadoPago:UnitPrice"] = "0"
        });

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.Null(exception);
    }

    [Fact]
    public void ShippedAppsettings_UsesUnitPriceFallback()
    {
        var options = _factory.Services.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;

        Assert.Equal(15000.00m, options.UnitPrice);
    }

    [Fact]
    public void MercadoPagoUnitPrice_EnvironmentOverrideWinsOverShippedFallback()
    {
        const string environmentVariable = "MercadoPago__UnitPrice";
        var originalValue = Environment.GetEnvironmentVariable(environmentVariable);
        Environment.SetEnvironmentVariable(environmentVariable, "17500.50");

        try
        {
            using var factory = _factory.WithConfiguration(new Dictionary<string, string?>());

            var options = factory.Services.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;

            Assert.Equal(17500.50m, options.UnitPrice);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, originalValue);
        }
    }

    [Fact]
    public void Disabled_InProduction_IsRejectedAtHostStartup()
    {
        using var factory = ProductionFactory(new Dictionary<string, string?>
        {
            ["MercadoPago:Mode"] = "Disabled",
            ["MercadoPago:AccessToken"] = "",
            ["MercadoPago:WebhookSecret"] = ""
        });

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.NotNull(exception);
        Assert.Contains("Mode must be explicitly set to Sandbox or Production", exception.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void MissingOrUnknownMode_IsRejectedAtHostStartup(string? mode)
    {
        using var factory = _factory.WithConfiguration(new Dictionary<string, string?>
        {
            ["MercadoPago:Mode"] = mode
        });

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.NotNull(exception);
        Assert.Contains("MercadoPago:Mode", exception.ToString());
    }

    [Fact]
    public void Production_WithCompleteValidConfiguration_Starts()
    {
        using var factory = ProductionFactory(new Dictionary<string, string?>
        {
            ["MercadoPago:Mode"] = "Production",
            ["MercadoPago:AccessToken"] = "production-token",
            ["MercadoPago:WebhookSecret"] = "production-webhook-secret",
            ["MercadoPago:UnitPrice"] = "4999.00"
        });

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Sandbox", "", "secret", "1")]
    [InlineData("Sandbox", "token", "", "1")]
    [InlineData("Sandbox", "token", "secret", "0")]
    [InlineData("Production", "", "secret", "1")]
    [InlineData("Production", "token", "", "1")]
    [InlineData("Production", "token", "secret", "-1")]
    public void EnabledMode_WithIncompleteConfiguration_IsRejectedAtHostStartup(
        string mode,
        string accessToken,
        string webhookSecret,
        string unitPrice)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["MercadoPago:Mode"] = mode,
            ["MercadoPago:AccessToken"] = accessToken,
            ["MercadoPago:WebhookSecret"] = webhookSecret,
            ["MercadoPago:UnitPrice"] = unitPrice
        };
        using var factory = mode == "Production"
            ? ProductionFactory(configuration)
            : _factory.WithConfiguration(configuration);

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.NotNull(exception);
        Assert.Contains("MercadoPago", exception.ToString());
    }

    private NidoTestWebAppFactory ProductionFactory(IReadOnlyDictionary<string, string?> configuration)
    {
        var productionConfiguration = new Dictionary<string, string?>(configuration)
        {
            ["Frontend:BaseUrl"] = "https://nidoapp.online",
            ["MercadoPago:ApiBaseUrl"] = "https://api.mercadopago.com"
        };

        return _factory.WithEnvironment("Production").WithConfiguration(productionConfiguration);
    }
}
