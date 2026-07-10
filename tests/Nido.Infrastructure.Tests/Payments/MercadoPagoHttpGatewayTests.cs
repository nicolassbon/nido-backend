using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nido.Application.Payments;
using Nido.Infrastructure.Payments;

namespace Nido.Infrastructure.Tests.Payments;

public sealed class MercadoPagoHttpGatewayTests
{
    [Fact]
    public async Task CreateCheckoutPreferenceAsync_WithExplicitPublicHttpsUrls_SendsConfiguredRedirectFieldsWithoutPerPreferenceWebhookUrl()
    {
        var handler = new CapturingHttpMessageHandler();
        var options = new MercadoPagoOptions
        {
            AccessToken = "test-token",
            UnitPrice = 1500m,
            SuccessUrl = "https://app.nido.test/perfil?status=success",
            PendingUrl = "https://app.nido.test/perfil?status=pending",
            FailureUrl = "https://app.nido.test/perfil?status=failure"
        };
        options.ApplyFrontendRedirectDefaults("https://frontend.example.com/");
        var gateway = CreateGateway(handler, options);
        var hogarId = Guid.NewGuid();

        await gateway.CreateCheckoutPreferenceAsync(new MercadoPagoCheckoutPreferenceRequest(hogarId), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.Equal(hogarId.ToString(), root.GetProperty("external_reference").GetString());
        Assert.Equal("approved", root.GetProperty("auto_return").GetString());
        Assert.Equal("https://app.nido.test/perfil?status=success", root.GetProperty("back_urls").GetProperty("success").GetString());
        Assert.Equal("https://app.nido.test/perfil?status=pending", root.GetProperty("back_urls").GetProperty("pending").GetString());
        Assert.Equal("https://app.nido.test/perfil?status=failure", root.GetProperty("back_urls").GetProperty("failure").GetString());
        Assert.False(root.TryGetProperty("notification_url", out _));
    }

    [Theory]
    [InlineData("https://app.example.com/")]
    [InlineData("https://app.example.com")]
    public async Task CreateCheckoutPreferenceAsync_WithPublicHttpsFrontendBaseUrl_SendsDerivedRedirectFields(string frontendBaseUrl)
    {
        var handler = new CapturingHttpMessageHandler();
        var options = new MercadoPagoOptions
        {
            AccessToken = "test-token",
            UnitPrice = 1500m
        };
        options.ApplyFrontendRedirectDefaults(frontendBaseUrl);
        var gateway = CreateGateway(handler, options);

        await gateway.CreateCheckoutPreferenceAsync(new MercadoPagoCheckoutPreferenceRequest(Guid.NewGuid()), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.Equal("approved", root.GetProperty("auto_return").GetString());
        Assert.Equal("https://app.example.com/perfil?status=success", root.GetProperty("back_urls").GetProperty("success").GetString());
        Assert.Equal("https://app.example.com/perfil?status=pending", root.GetProperty("back_urls").GetProperty("pending").GetString());
        Assert.Equal("https://app.example.com/perfil?status=failure", root.GetProperty("back_urls").GetProperty("failure").GetString());
    }

    [Theory]
    [InlineData("http://localhost:4200", "http://localhost:4200")]
    [InlineData("http://127.0.0.1:4200", "http://127.0.0.1:4200")]
    [InlineData("http://[::1]:4200", "http://[::1]:4200")]
    [InlineData("https://localhost:4200", "https://localhost:4200")]
    public async Task CreateCheckoutPreferenceAsync_WithLoopbackFrontendBaseUrl_OmitsRedirectFieldsAndPerPreferenceWebhookUrl(
        string frontendBaseUrl,
        string expectedBaseUrl)
    {
        var handler = new CapturingHttpMessageHandler();
        var options = new MercadoPagoOptions
        {
            AccessToken = "test-token"
        };
        options.ApplyFrontendRedirectDefaults(frontendBaseUrl);
        var gateway = CreateGateway(handler, options);

        await gateway.CreateCheckoutPreferenceAsync(new MercadoPagoCheckoutPreferenceRequest(Guid.NewGuid()), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.False(root.TryGetProperty("back_urls", out _));
        Assert.False(root.TryGetProperty("auto_return", out _));
        Assert.False(root.TryGetProperty("notification_url", out _));
        Assert.Equal($"{expectedBaseUrl}/perfil?status=success", options.SuccessUrl);
        Assert.Equal($"{expectedBaseUrl}/perfil?status=pending", options.PendingUrl);
        Assert.Equal($"{expectedBaseUrl}/perfil?status=failure", options.FailureUrl);
    }

    [Fact]
    public void ApplyFrontendRedirectDefaults_WithExplicitUrls_KeepsConfiguredValues()
    {
        var options = new MercadoPagoOptions
        {
            SuccessUrl = "https://configured.example.com/success",
            PendingUrl = "https://configured.example.com/pending",
            FailureUrl = "https://configured.example.com/failure"
        };

        options.ApplyFrontendRedirectDefaults("https://frontend.example.com");

        Assert.Equal("https://configured.example.com/success", options.SuccessUrl);
        Assert.Equal("https://configured.example.com/pending", options.PendingUrl);
        Assert.Equal("https://configured.example.com/failure", options.FailureUrl);
    }

    [Fact]
    public void ApplyFrontendRedirectDefaults_WithBlankExplicitUrls_DerivesValues()
    {
        var options = new MercadoPagoOptions
        {
            SuccessUrl = " ",
            PendingUrl = string.Empty,
            FailureUrl = "\t"
        };

        options.ApplyFrontendRedirectDefaults("https://frontend.example.com");

        Assert.Equal("https://frontend.example.com/perfil?status=success", options.SuccessUrl);
        Assert.Equal("https://frontend.example.com/perfil?status=pending", options.PendingUrl);
        Assert.Equal("https://frontend.example.com/perfil?status=failure", options.FailureUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    public void ApplyFrontendRedirectDefaults_WithInvalidOrBlankBaseUrl_DoesNotDeriveValues(string? frontendBaseUrl)
    {
        var options = new MercadoPagoOptions();

        options.ApplyFrontendRedirectDefaults(frontendBaseUrl);

        Assert.Equal(string.Empty, options.SuccessUrl);
        Assert.Equal(string.Empty, options.PendingUrl);
        Assert.Equal(string.Empty, options.FailureUrl);
    }

    [Theory]
    [InlineData("https://frontend.example.com/")]
    [InlineData("https://frontend.example.com")]
    public void ApplyFrontendRedirectDefaults_WithTrailingOrNoTrailingSlash_DerivesSameValues(string frontendBaseUrl)
    {
        var options = new MercadoPagoOptions();

        options.ApplyFrontendRedirectDefaults(frontendBaseUrl);

        Assert.Equal("https://frontend.example.com/perfil?status=success", options.SuccessUrl);
        Assert.Equal("https://frontend.example.com/perfil?status=pending", options.PendingUrl);
        Assert.Equal("https://frontend.example.com/perfil?status=failure", options.FailureUrl);
    }

    [Fact]
    public async Task CreateCheckoutPreferenceAsync_WithIncompleteOrEmptyUrls_OmitsRedirectFields()
    {
        var handler = new CapturingHttpMessageHandler();
        var gateway = CreateGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "test-token",
            SuccessUrl = "http://localhost:4200/perfil?status=success",
            PendingUrl = "",
            FailureUrl = "http://localhost:4200/perfil?status=failure"
        });

        await gateway.CreateCheckoutPreferenceAsync(new MercadoPagoCheckoutPreferenceRequest(Guid.NewGuid()), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.False(root.TryGetProperty("back_urls", out _));
        Assert.False(root.TryGetProperty("auto_return", out _));
        Assert.False(root.TryGetProperty("notification_url", out _));
    }

    [Fact]
    public async Task CreateCheckoutPreferenceAsync_WhenMercadoPagoReturnsError_ExposesOnlySafeStatusAndErrorCode()
    {
        var handler = new CapturingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"message\":\"payer@example.test could not be charged\",\"error\":\"bad_request\",\"status\":400,\"checkout_url\":\"https://provider.test/checkout?token=raw-response-token\"}");
        var gateway = CreateGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "test-token",
            UnitPrice = 1500m,
            SuccessUrl = "http://localhost:4200/perfil?status=success",
            PendingUrl = "http://localhost:4200/perfil?status=pending",
            FailureUrl = "http://localhost:4200/perfil?status=failure"
        });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            gateway.CreateCheckoutPreferenceAsync(new MercadoPagoCheckoutPreferenceRequest(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("400", exception.Message);
        Assert.Contains("bad_request", exception.Message);
        Assert.DoesNotContain("payer@example.test", exception.Message);
        Assert.DoesNotContain("raw-response-token", exception.Message);
    }

    [Fact]
    public async Task GetPaymentAsync_WithApprovedPayload_ParsesProviderFields()
    {
        var hogarId = Guid.NewGuid();
        var gateway = CreateGateway(new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            $"{{\"external_reference\":\"{hogarId}\",\"status\":\"approved\",\"preapproval_id\":\"sub-1\",\"date_approved\":\"2026-07-10T12:00:00Z\",\"date_last_updated\":\"2026-07-10T12:05:00Z\"}}"),
            new MercadoPagoOptions { AccessToken = "test-token" });

        var payment = await gateway.GetPaymentAsync("payment-1", CancellationToken.None);

        Assert.Equal("payment-1", payment.ProviderPaymentId);
        Assert.Equal(hogarId, payment.HogarId);
        Assert.Equal("approved", payment.Status);
        Assert.Equal("sub-1", payment.ProviderSubscriptionId);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), payment.DateApproved);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 5, 0, DateTimeKind.Utc), payment.ProviderTransitionAt);
    }

    [Fact]
    public async Task GetPaymentAsync_WithNullableApprovalAndSubscription_ParsesNullValues()
    {
        var hogarId = Guid.NewGuid();
        var gateway = CreateGateway(new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            $"{{\"external_reference\":\"{hogarId}\",\"status\":\"pending\",\"preapproval_id\":null,\"date_approved\":null}}"),
            new MercadoPagoOptions { AccessToken = "test-token" });

        var payment = await gateway.GetPaymentAsync("payment-pending", CancellationToken.None);

        Assert.Null(payment.ProviderSubscriptionId);
        Assert.Null(payment.DateApproved);
        Assert.Null(payment.ProviderTransitionAt);
    }

    [Theory]
    [InlineData("2026-07-10T12:00:00Z", "2026-07-10T12:00:00Z")]
    [InlineData("2026-07-10T09:00:00-03:00", "2026-07-10T12:00:00Z")]
    public async Task GetPaymentAsync_WithExplicitProviderOffset_NormalizesApprovalDateToUtc(string providerDate, string expectedUtc)
    {
        var hogarId = Guid.NewGuid();
        var gateway = CreateGateway(new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            $"{{\"external_reference\":\"{hogarId}\",\"status\":\"approved\",\"date_approved\":\"{providerDate}\"}}"),
            new MercadoPagoOptions { AccessToken = "test-token" });

        var payment = await gateway.GetPaymentAsync("payment-with-offset", CancellationToken.None);

        Assert.Equal(DateTime.Parse(expectedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind), payment.DateApproved);
    }

    [Theory]
    [InlineData("2026-07-10T12:00:00")]
    [InlineData("2026-07-10 12:00:00")]
    public async Task GetPaymentAsync_WithOffsetlessApprovalDate_IgnoresDateRatherThanUsingHostTimezone(string providerDate)
    {
        var hogarId = Guid.NewGuid();
        var gateway = CreateGateway(new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            $"{{\"external_reference\":\"{hogarId}\",\"status\":\"approved\",\"date_approved\":\"{providerDate}\"}}"),
            new MercadoPagoOptions { AccessToken = "test-token" });

        var payment = await gateway.GetPaymentAsync("payment-without-offset", CancellationToken.None);

        Assert.Null(payment.DateApproved);
    }

    [Fact]
    public async Task GetPaymentAsync_WithInvalidExternalReference_ThrowsInvalidOperationException()
    {
        var gateway = CreateGateway(new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            "{\"external_reference\":\"invalid\",\"status\":\"approved\"}"),
            new MercadoPagoOptions { AccessToken = "test-token" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.GetPaymentAsync("payment-invalid", CancellationToken.None));

        Assert.Contains("Invalid HogarId", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GetPaymentAsync_WithNonSuccessResponse_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode statusCode)
    {
        var gateway = CreateGateway(new CapturingHttpMessageHandler(statusCode, "{}"), new MercadoPagoOptions { AccessToken = "test-token" });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => gateway.GetPaymentAsync("payment-error", CancellationToken.None));

        Assert.Equal(statusCode, exception.StatusCode);
    }

    private static MercadoPagoHttpGateway CreateGateway(CapturingHttpMessageHandler handler, MercadoPagoOptions options)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.ApiBaseUrl)
        };
        return new MercadoPagoHttpGateway(client, Options.Create(options));
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public CapturingHttpMessageHandler(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string responseBody = "{\"id\":\"pref-test\",\"init_point\":\"https://mercadopago.test/checkout\"}")
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
        }
    }
}
