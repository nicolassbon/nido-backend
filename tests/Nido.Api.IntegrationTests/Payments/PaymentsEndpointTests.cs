using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nido.Api.Controllers;
using Nido.Api.IntegrationTests.Auth;
using Nido.Application.Common.Security;
using Nido.Application.Payments;
using Nido.Infrastructure.Persistence;
using Nido.Tests.Shared;

namespace Nido.Api.IntegrationTests.Payments;

public sealed class PaymentsEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public PaymentsEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCheckout_WhenAuthenticated_ReturnsPreferenceAndUsesCurrentHousehold()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-preference");

        var response = await client.PostAsync("/api/payments/checkout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckoutPreferenceBody>();
        Assert.NotNull(body);
        Assert.Equal("pref_test", body!.PreferenceId);
        Assert.Equal("https://checkout.test.local/init", body.InitPoint);
        Assert.Equal(user.HogarId, gateway.LastCheckoutHogarId);
    }

    [Fact]
    public async Task GetSubscription_WhenAuthenticated_ReturnsCurrentHouseholdPlanState()
    {
        using var client = _factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-subscription");
        var trialEndsAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
        var subscriptionEndsAt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        await MakePremiumAsync(user.HogarId, trialEndsAt, subscriptionEndsAt);

        var response = await client.GetAsync("/api/payments/subscription");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.NotNull(body);
        Assert.Equal("premium", body!.Plan);
        Assert.Equal("active", body.SubscriptionStatus);
        Assert.NotNull(body.TrialEndsAt);
        Assert.NotNull(body.SubscriptionEndsAt);
        Assert.Equal(subscriptionEndsAt, body.SubscriptionEndsAt.Value);
        Assert.Equal(trialEndsAt, body.TrialEndsAt.Value);
    }

    [Fact]
    public async Task ExpiredSubscription_RefreshSubscriptionAndPremiumGateProjectFreeEntitlement()
    {
        using var client = _factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-expired-entitlement");
        await MakePremiumAsync(
            user.HogarId,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddMinutes(-1));

        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refresh = await refreshResponse.Content.ReadFromJsonAsync<RefreshBody>();
        Assert.NotNull(refresh);
        Assert.Equal("free", refresh!.Plan);
        Assert.Equal("free", refresh.SubscriptionStatus);
        Assert.DoesNotContain(
            new JwtSecurityTokenHandler().ReadJwtToken(refresh.AccessToken).Claims,
            claim => claim.Type == ClaimTypes.SubscriptionEndsAt);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refresh.AccessToken);
        var subscriptionResponse = await client.GetAsync("/api/payments/subscription");
        var subscription = await subscriptionResponse.Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.Equal(HttpStatusCode.OK, subscriptionResponse.StatusCode);
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription!.Plan);
        Assert.Equal("free", subscription.SubscriptionStatus);
        Assert.Null(subscription.TrialEndsAt);
        Assert.Null(subscription.SubscriptionEndsAt);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true })).StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenSignatureInvalidAndPayloadMalformed_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/mercadopago?data.id=payment-1")
        {
            Content = new StringContent("{ malformed json")
        };
        request.Headers.Add("x-signature", "ts=1700000000,v1=invalid");
        request.Headers.Add("x-request-id", "request-1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenSignatureInvalid_LogsSafeDiagnosticsWithoutSensitiveMaterial()
    {
        const string webhookSecret = "raw-test-webhook-secret-that-must-not-appear-in-logs";
        const string fullSignature = "ts=1700000000,v1=1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var logCapture = new TestLogCapture();
        using var factory = _factory
            .WithConfiguration(new Dictionary<string, string?>
            {
                ["MercadoPago:WebhookSecret"] = webhookSecret
            })
            .WithLogCapture(logCapture);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/webhooks/mercadopago?data.id=payment-query&id=merchant-order&type=payment")
        {
            Content = new StringContent(
                "{\"id\":\"event-body\",\"type\":\"payment\",\"data\":{\"id\":\"payment-body\"}}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("x-signature", fullSignature);
        request.Headers.Add("x-request-id", "request-safe-diagnostics");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var log = Assert.Single(
            logCapture.EntriesForCategoryContaining(nameof(PaymentsWebhookController)),
            entry => entry.Level == LogLevel.Warning);
        Assert.Contains("Unauthorized Mercado Pago webhook", log.Message);
        Assert.DoesNotContain(webhookSecret, log.Message);
        Assert.DoesNotContain(fullSignature, log.Message);
        Assert.DoesNotContain("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", log.Message);
        Assert.DoesNotContain("SecretLength", log.Message);
        Assert.DoesNotContain("SecretFingerprint", log.Message);
        Assert.DoesNotContain("ReceivedV1Prefix", log.Message);
        Assert.DoesNotContain("RawSignatureHeader", log.Message);
        Assert.DoesNotContain("CandidateManifestHmacPrefixes", log.Message);
        Assert.Contains("payment-query", log.Message);
        Assert.DoesNotContain("payment-body", log.Message);
    }

    [Fact]
    public async Task Webhook_WhenSignatureInvalid_ReturnsUnauthorized()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        using var request = BuildInvalidWebhookRequest("payment-invalid-default", "request-invalid-default");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WithValidSignatureAndDataIdQuery_ReturnsOk()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-data-id",
            dataIdForSignature: "payment-data-id",
            requestId: "request-data-id",
            payload: BuildWebhookPayload("event-data-id", "payment-data-id"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("payment-data-id", gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WithValidSignatureAndFallbackIdQuery_ReturnsOk()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?id=payment-fallback-id",
            dataIdForSignature: "payment-fallback-id",
            requestId: "request-fallback-id",
            payload: BuildWebhookPayload("event-fallback-id", "payment-fallback-id"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("payment-fallback-id", gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WithDataIdAndIdQueries_UsesDataIdForSignature()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-preferred&id=merchant-order-ignored",
            dataIdForSignature: "payment-preferred",
            requestId: "request-preferred",
            payload: BuildWebhookPayload("event-preferred", "payment-preferred"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("payment-preferred", gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WhenPaymentLookupFails_ReturnsServiceUnavailableSoMercadoPagoRetries()
    {
        var gateway = new FakeMercadoPagoGateway
        {
            PaymentLookupException = new HttpRequestException("Mercado Pago unavailable", null, HttpStatusCode.BadGateway)
        };
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-retry",
            "payment-retry",
            "request-retry",
            BuildWebhookPayload("event-retry", "payment-retry"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("payment-retry", gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WithValidPayment_UpdatesSubscriptionAndDuplicateDoesNotCreateSecondEvent()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-webhook-contract");
        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-contract",
            user.HogarId,
            "approved",
            null,
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        const string payload = "{\"id\":\"event-contract\",\"type\":\"payment\",\"data\":{\"id\":\"payment-contract\"}}";

        using var firstRequest = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-contract",
            "payment-contract",
            "request-contract-1",
            payload);
        var firstResponse = await client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var subscriptionResponse = await client.GetAsync("/api/payments/subscription");
        var subscription = await subscriptionResponse.Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.Equal(HttpStatusCode.OK, subscriptionResponse.StatusCode);
        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
        Assert.NotNull(subscription.SubscriptionEndsAt);
        Assert.Equal(1, await GetWebhookEventCountAsync(factory, user.HogarId));

        var premiumEndpointResponse = await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true });
        Assert.Equal(HttpStatusCode.OK, premiumEndpointResponse.StatusCode);

        using var duplicateRequest = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-contract",
            "payment-contract",
            "request-contract-2",
            payload);
        var duplicateResponse = await client.SendAsync(duplicateRequest);

        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        Assert.Equal(1, await GetWebhookEventCountAsync(factory, user.HogarId));
        Assert.Equal(2, gateway.PaymentLookupCount);

        var entitlementAfterDuplicate = await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = false });
        Assert.Equal(HttpStatusCode.OK, entitlementAfterDuplicate.StatusCode);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    public async Task Webhook_WhenActivePremiumHouseholdReceivesNonActivePayment_PreservesActivePremiumState(string paymentStatus)
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, $"payments-regression-{paymentStatus}");
        await MakePremiumAsync(factory, user.HogarId, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(30));
        gateway.PaymentDetails = new MercadoPagoPaymentDetails($"payment-{paymentStatus}", user.HogarId, paymentStatus, null);
        using var request = BuildWebhookRequest(
            $"/api/webhooks/mercadopago?data.id=payment-{paymentStatus}",
            $"payment-{paymentStatus}",
            $"request-{paymentStatus}",
            BuildWebhookPayload($"event-{paymentStatus}", $"payment-{paymentStatus}"));

        var response = await client.SendAsync(request);
        var subscriptionResponse = await client.GetAsync("/api/payments/subscription");
        var subscription = await subscriptionResponse.Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
        Assert.Equal(1, await GetWebhookEventCountAsync(factory, user.HogarId));
    }

    [Fact]
    public async Task Webhook_WhenPayloadExceedsLimit_ReturnsPayloadTooLargeBeforeReadingBody()
    {
        using var client = _factory.CreateClient();
        var oversizedPayload = new string('x', 64 * 1024 + 1);
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-oversized",
            "payment-oversized",
            "request-oversized",
            oversizedPayload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenChunkedPayloadExceedsLimit_ReturnsPayloadTooLargeWithoutLookupOrPersistence()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-chunked-oversized");
        var oversizedPayload = new string('x', 64 * 1024 + 1);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/mercadopago?data.id=payment-chunked-oversized")
        {
            Content = new ChunkedContent(oversizedPayload)
        };
        request.Headers.TransferEncodingChunked = true;
        request.Headers.Add("x-signature", BuildMercadoPagoSignature("payment-chunked-oversized", "request-chunked-oversized", "1700000000", "test-mercadopago-webhook-secret"));
        request.Headers.Add("x-request-id", "request-chunked-oversized");

        Assert.Null(request.Content.Headers.ContentLength);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Null(gateway.LastPaymentId);
        Assert.Equal(0, await GetWebhookEventCountAsync(factory, user.HogarId));
    }

    [Fact]
    public async Task Webhook_WhenPayloadIsExactlyLimit_ReachesWebhookHandler()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var payload = BuildWebhookPayload("event-exact-limit", "payment-exact-limit");
        payload = payload.PadRight(64 * 1024);
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-exact-limit",
            "payment-exact-limit",
            "request-exact-limit",
            payload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("payment-exact-limit", gateway.LastPaymentId);
    }

    [Fact]
    public async Task Webhook_WhenSignedIdDiffersFromPayloadId_ReturnsOkWithoutLookupOrMutation()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-signed-mismatch");
        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-signed",
            user.HogarId,
            "approved",
            "subscription-signed",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-signed",
            "payment-signed",
            "request-signed-mismatch",
            BuildWebhookPayload("event-signed-mismatch", "payment-body"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(gateway.LastPaymentId);
        Assert.Equal(0, await GetWebhookEventCountAsync(factory, user.HogarId));
    }

    [Fact]
    public async Task Webhook_WhenOlderCancellationArrivesAfterNewApproval_PreservesCurrentPremiumEntitlement()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-stale-cancellation");

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-new",
            user.HogarId,
            "approved",
            "subscription-new",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-new",
            "payment-new",
            "request-new",
            BuildWebhookPayload("event-new", "payment-new"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-old",
            user.HogarId,
            "cancelled",
            "subscription-old",
            null,
            new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-old",
            "payment-old",
            "request-old",
            BuildWebhookPayload("event-old", "payment-old"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        var subscriptionResponse = await client.GetAsync("/api/payments/subscription");
        var subscription = await subscriptionResponse.Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
        Assert.Equal(2, await GetWebhookEventCountAsync(factory, user.HogarId));
    }

    [Fact]
    public async Task Webhook_WhenOldPaymentCancellationHasLaterTransition_PreservesNewerPremiumEntitlement()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-mismatched-later-cancellation");

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-newer", user.HogarId, "approved", null,
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-newer", "payment-newer", "request-newer",
            BuildWebhookPayload("event-newer", "payment-newer"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-old", user.HogarId, "cancelled", null, null,
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-old", "payment-old", "request-old-later",
            BuildWebhookPayload("event-old-later", "payment-old"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
    }

    [Fact]
    public async Task Webhook_LegacyFeedApprovalThenCancellationWithoutEventId_RevokesPremium()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-legacy-feed-transitions");
        const string paymentId = "payment-legacy-feed-transitions";
        const string payload = "{\"resource\":\"payment-legacy-feed-transitions\",\"topic\":\"payment\"}";

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            paymentId, user.HogarId, "approved", null,
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            $"/api/webhooks/mercadopago?data.id={paymentId}", paymentId, "request-legacy-feed-approval", payload);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            paymentId, user.HogarId, "cancelled", null, null,
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            $"/api/webhooks/mercadopago?data.id={paymentId}", paymentId, "request-legacy-feed-cancellation", payload);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.NotNull(subscription);
        Assert.Equal("free", subscription!.Plan);
        Assert.Equal("cancelled", subscription.SubscriptionStatus);
        Assert.Equal(2, await GetWebhookEventCountAsync(factory, user.HogarId));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true })).StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenCancellationSharesSubscriptionButHasOlderPayment_PreservesCurrentPremiumEntitlement()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-same-subscription-stale-cancellation");

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-current",
            user.HogarId,
            "approved",
            "subscription-shared",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-current",
            "payment-current",
            "request-current",
            BuildWebhookPayload("event-current", "payment-current"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-old",
            user.HogarId,
            "cancelled",
            "subscription-shared",
            null,
            new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-old",
            "payment-old",
            "request-old-shared-subscription",
            BuildWebhookPayload("event-old-shared-subscription", "payment-old"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
    }

    [Fact]
    public async Task Webhook_WhenCurrentPaymentIsCancelled_RevokesPremiumAndPremiumEndpointReturnsForbidden()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-current-cancellation");

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-current-cancellation",
            user.HogarId,
            "approved",
            "subscription-current-cancellation",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-current-cancellation",
            "payment-current-cancellation",
            "request-current-cancellation-approval",
            BuildWebhookPayload("event-current-cancellation-approval", "payment-current-cancellation"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true })).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-current-cancellation",
            user.HogarId,
            "cancelled",
            "subscription-current-cancellation",
            null,
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-current-cancellation",
            "payment-current-cancellation",
            "request-current-cancellation-revocation",
            BuildWebhookPayload("event-current-cancellation-revocation", "payment-current-cancellation"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();

        Assert.NotNull(subscription);
        Assert.Equal("free", subscription!.Plan);
        Assert.Equal("cancelled", subscription.SubscriptionStatus);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = false })).StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenSameSignedEventArrivesConcurrently_StoresOneEventAndKeepsCorrectEntitlement()
    {
        var gateway = new FakeMercadoPagoGateway { ConcurrentLookupParticipantCount = 2 };
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var authenticatedClient = factory.CreateClient();
        var user = await AuthenticateAsync(authenticatedClient, "payments-concurrent-duplicate");
        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-concurrent-duplicate",
            user.HogarId,
            "approved",
            "subscription-concurrent-duplicate",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        const string payload = "{\"id\":\"event-concurrent-duplicate\",\"type\":\"payment\",\"data\":{\"id\":\"payment-concurrent-duplicate\"}}";

        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        var responses = await Task.WhenAll(
            SendWebhookAsync(firstClient, "request-concurrent-duplicate-1", payload),
            SendWebhookAsync(secondClient, "request-concurrent-duplicate-2", payload));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(1, await GetWebhookEventCountAsync(factory, user.HogarId));

        var subscription = await (await authenticatedClient.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.NotNull(subscription);
        Assert.Equal("premium", subscription!.Plan);
        Assert.Equal("active", subscription.SubscriptionStatus);
    }

    [Fact]
    public async Task Webhook_WhenStaleApprovalArrivesAfterCancellation_PreservesCancelledEntitlement()
    {
        var gateway = new FakeMercadoPagoGateway();
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-stale-approval");

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-cancelled", user.HogarId, "cancelled", "subscription-1", null,
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc));
        using var cancellation = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-cancelled", "payment-cancelled", "request-cancelled",
            BuildWebhookPayload("event-cancelled", "payment-cancelled"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(cancellation)).StatusCode);

        gateway.PaymentDetails = new MercadoPagoPaymentDetails(
            "payment-stale", user.HogarId, "approved", "subscription-1",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        using var approval = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-stale", "payment-stale", "request-stale",
            BuildWebhookPayload("event-stale", "payment-stale"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(approval)).StatusCode);

        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription!.Plan);
        Assert.Equal("cancelled", subscription.SubscriptionStatus);
    }

    [Fact]
    public async Task Webhook_WhenDistinctTransitionsArriveConcurrently_PersistsNewestProviderTransition()
    {
        var gateway = new FakeMercadoPagoGateway { ConcurrentLookupParticipantCount = 2 };
        using var factory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IMercadoPagoGateway>();
            services.AddSingleton<IMercadoPagoGateway>(gateway);
        });
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client, "payments-concurrent-transitions");
        gateway.PaymentDetailsById = new Dictionary<string, MercadoPagoPaymentDetails>
        {
            ["payment-approved"] = new("payment-current", user.HogarId, "approved", "subscription-1", new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)),
            ["payment-cancelled"] = new("payment-current", user.HogarId, "cancelled", "subscription-1", null, new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc))
        };

        async Task<HttpResponseMessage> SendAsync(string paymentId, string eventId)
        {
            using var request = BuildWebhookRequest(
                $"/api/webhooks/mercadopago?data.id={paymentId}", paymentId, $"request-{eventId}",
                BuildWebhookPayload(eventId, paymentId));
            return await client.SendAsync(request);
        }

        var responses = await Task.WhenAll(
            SendAsync("payment-approved", "event-concurrent-approved"),
            SendAsync("payment-cancelled", "event-concurrent-cancelled"));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var subscription = await (await client.GetAsync("/api/payments/subscription"))
            .Content.ReadFromJsonAsync<SubscriptionBody>();
        Assert.NotNull(subscription);
        Assert.Equal("free", subscription!.Plan);
        Assert.Equal("cancelled", subscription.SubscriptionStatus);
    }

    [Fact]
    public void WebhookSignatureManifestDataId_PrefersDataIdQueryAndFallsBackToIdQuery()
    {
        var dataIdOnly = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["data.id"] = "payment-1",
            ["id"] = "merchant-order-1"
        });
        var idOnly = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "payment-2"
        });

        Assert.Equal("payment-1", PaymentsWebhookController.ResolveSignatureManifestDataId(dataIdOnly));
        Assert.Equal("payment-2", PaymentsWebhookController.ResolveSignatureManifestDataId(idOnly));
    }

    private async Task<RegisterBody> AuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return body;
    }

    private async Task MakePremiumAsync(Guid hogarId, DateTime trialEndsAt, DateTime? subscriptionEndsAt = null)
        => await MakePremiumAsync(_factory, hogarId, trialEndsAt, subscriptionEndsAt);

    private static async Task MakePremiumAsync(NidoTestWebAppFactory factory, Guid hogarId, DateTime trialEndsAt, DateTime? subscriptionEndsAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var hogar = await db.Hogares.SingleAsync(x => x.Id == hogarId);
        hogar.Plan = "premium";
        hogar.SubscriptionStatus = "active";
        hogar.TrialEndsAt = trialEndsAt;
        hogar.SuscripcionVenceEl = subscriptionEndsAt;
        hogar.PlanUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task<int> GetWebhookEventCountAsync(NidoTestWebAppFactory factory, Guid hogarId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        return await db.PaymentWebhookEvents.CountAsync(eventRecord => eventRecord.HogarId == hogarId);
    }

    private static HttpRequestMessage BuildWebhookRequest(string requestUri, string dataIdForSignature, string requestId, string payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-signature", BuildMercadoPagoSignature(dataIdForSignature, requestId, "1700000000", "test-mercadopago-webhook-secret"));
        request.Headers.Add("x-request-id", requestId);

        return request;
    }

    private static HttpRequestMessage BuildInvalidWebhookRequest(string dataId, string requestId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webhooks/mercadopago?data.id={dataId}")
        {
            Content = new StringContent(BuildWebhookPayload($"event-{dataId}", dataId), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-signature", "ts=1700000000,v1=invalid");
        request.Headers.Add("x-request-id", requestId);

        return request;
    }

    private static async Task<HttpResponseMessage> SendWebhookAsync(HttpClient client, string requestId, string payload)
    {
        using var request = BuildWebhookRequest(
            "/api/webhooks/mercadopago?data.id=payment-concurrent-duplicate",
            "payment-concurrent-duplicate",
            requestId,
            payload);
        return await client.SendAsync(request);
    }

    private static string BuildWebhookPayload(string eventId, string dataId)
        => $"{{\"id\":\"{eventId}\",\"type\":\"payment\",\"data\":{{\"id\":\"{dataId}\"}}}}";

    private static string BuildMercadoPagoSignature(string dataId, string requestId, string timestamp, string secret)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return $"ts={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed class FakeMercadoPagoGateway : IMercadoPagoGateway
    {
        private readonly TaskCompletionSource _concurrentLookupBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _concurrentLookupCount;

        public Guid? LastCheckoutHogarId { get; private set; }
        public string? LastPaymentId { get; private set; }
        public int PaymentLookupCount { get; private set; }
        public int ConcurrentLookupParticipantCount { get; init; }
        public MercadoPagoPaymentDetails? PaymentDetails { get; set; }
        public IReadOnlyDictionary<string, MercadoPagoPaymentDetails>? PaymentDetailsById { get; set; }
        public Exception? PaymentLookupException { get; set; }

        public Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(MercadoPagoCheckoutPreferenceRequest request, CancellationToken ct)
        {
            LastCheckoutHogarId = request.HogarId;
            return Task.FromResult(new MercadoPagoCheckoutPreference(
                "pref_test",
                new Uri("https://checkout.test.local/init")));
        }

        public async Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct)
        {
            LastPaymentId = paymentId;
            PaymentLookupCount++;
            if (ConcurrentLookupParticipantCount > 0)
            {
                if (Interlocked.Increment(ref _concurrentLookupCount) == ConcurrentLookupParticipantCount)
                {
                    _concurrentLookupBarrier.TrySetResult();
                }

                await _concurrentLookupBarrier.Task.WaitAsync(ct);
            }

            if (PaymentLookupException is not null)
            {
                throw PaymentLookupException;
            }

            var payment = PaymentDetailsById?.GetValueOrDefault(paymentId) ?? PaymentDetails ?? new MercadoPagoPaymentDetails(
                paymentId,
                Guid.NewGuid(),
                "approved",
                null,
                new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
            return payment.ProviderTransitionAt.HasValue
                ? payment
                : payment with { ProviderTransitionAt = payment.DateApproved ?? new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc) };
        }
    }

    private sealed class ChunkedContent : HttpContent
    {
        private readonly byte[] _payload;

        public ChunkedContent(string payload)
        {
            _payload = Encoding.UTF8.GetBytes(payload);
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(_payload).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record CheckoutPreferenceBody(string PreferenceId, string InitPoint);
    private sealed record SubscriptionBody(string Plan, string SubscriptionStatus, DateTime? TrialEndsAt, DateTime? SubscriptionEndsAt);
    private sealed record RefreshBody(string AccessToken, string Plan, string SubscriptionStatus, DateTime? TrialEndsAt);
}
