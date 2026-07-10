using System.Net;
using Nido.Application.Payments;

namespace Nido.Application.Tests.Payments;

public sealed class ProcessWebhookHandlerTests
{
    private const string Secret = "test-webhook-secret";

    [Fact]
    public async Task Handle_InvalidSignature_ReturnsUnauthorizedAndDoesNotStoreEvent()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-1", hogarId, "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-1", "payment", "payment-1");

        var result = await handler.Handle(new ProcessWebhookCommand(
            Payload: payload,
            Signature: "ts=1700000000,v1=invalid",
            RequestId: "request-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Unauthorized, result.Outcome);
        Assert.Equal(0, repository.RecordAttempts);
        Assert.Null(repository.LastPlanUpdate);
    }

    [Fact]
    public async Task Handle_InvalidSignatureWithMalformedPayload_ReturnsUnauthorizedWithoutReadingPayload()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-1", hogarId, "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });

        var result = await handler.Handle(new ProcessWebhookCommand(
            Payload: "{ malformed json",
            Signature: "ts=1700000000,v1=invalid",
            RequestId: "request-1",
            DataId: "payment-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Unauthorized, result.Outcome);
        Assert.Equal(0, repository.RecordAttempts);
        Assert.Null(repository.LastPlanUpdate);
        Assert.Equal(0, gateway.PaymentLookupCount);
    }

    [Fact]
    public async Task Handle_DuplicateEvent_ReturnsDuplicateWithoutChangingState()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository { DuplicateEventIds = { "event-1" } };
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-1", hogarId, "approved", null, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-1", "payment", "payment-1");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-1", "request-1", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-1", "payment-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Duplicate, result.Outcome);
        Assert.Null(repository.LastPlanUpdate);
        Assert.Equal(1, gateway.PaymentLookupCount);
    }

    [Fact]
    public async Task Handle_ApprovedPayment_GrantsPremiumAndMarksEventProcessed()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var approvedAt = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-1", hogarId, "approved", null, approvedAt, approvedAt));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-1", "payment", "payment-1");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-1", "request-1", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-1", "payment-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Processed, result.Outcome);
        Assert.NotNull(repository.LastPlanUpdate);
        Assert.Equal(hogarId, repository.LastPlanUpdate.HogarId);
        Assert.Equal(HouseholdPlan.Premium, repository.LastPlanUpdate.Plan);
        Assert.Equal(SubscriptionStatus.Active, repository.LastPlanUpdate.SubscriptionStatus);
        Assert.Equal("payment-1", repository.LastPlanUpdate.ProviderPaymentId);
        Assert.Null(repository.LastPlanUpdate.ProviderSubscriptionId);
        Assert.NotNull(repository.LastPlanUpdate.SubscriptionEndsAt);
        Assert.Equal(approvedAt.AddDays(30), repository.LastPlanUpdate.SubscriptionEndsAt);
        Assert.Contains("event-1", repository.ProcessedEventIds);
    }

    [Fact]
    public async Task Handle_CancelledPayment_RevertsHouseholdToFree()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var transitionAt = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("subscription-1", hogarId, "cancelled", "subscription-1", null, transitionAt));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-2", "payment", "subscription-1");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("subscription-1", "request-2", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-2", "subscription-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Processed, result.Outcome);
        Assert.Equal(new PaymentPlanUpdate(hogarId, HouseholdPlan.Free, SubscriptionStatus.Cancelled, "subscription-1", "subscription-1", null, transitionAt), repository.LastPlanUpdate);
        Assert.Contains("event-2", repository.ProcessedEventIds);
    }

    [Fact]
    public async Task Handle_SignedPaymentIdDoesNotMatchPayload_ReturnsIgnoredWithoutLookupOrMutation()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-signed", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-signed", "request-mismatch", "1700000000", Secret);

        var result = await handler.Handle(
            new ProcessWebhookCommand(BuildPayload("event-mismatch", "payment", "payment-body"), signature, "request-mismatch", "payment-signed"),
            CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(0, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_ActivePaymentWithoutApprovalDate_ReturnsIgnoredWithoutMutation()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-no-date", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-no-date", "payment", "payment-no-date");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-no-date", "request-no-date", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-no-date", "payment-no-date"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_FeedV2PaymentPayload_UsesResourceAsPaymentIdAndProcessesEvent()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-feed-1", hogarId, "approved", null, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = "{\"resource\":\"payment-feed-1\",\"topic\":\"payment\"}";
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-feed-1", "request-feed", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-feed", "payment-feed-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Processed, result.Outcome);
        Assert.Equal("payment-feed-1", gateway.LastPaymentId);
        Assert.Equal(HouseholdPlan.Premium, repository.LastPlanUpdate?.Plan);
        Assert.Single(repository.ProcessedEventIds, eventId => eventId.StartsWith("legacy:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_LegacyFeedTransitionsWithoutEventId_UseDistinctTransitionSpecificIds()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails(
            "payment-feed-transition", hogarId, "approved", null,
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        const string payload = "{\"resource\":\"payment-feed-transition\",\"topic\":\"payment\"}";

        var approval = await handler.Handle(new ProcessWebhookCommand(
            payload,
            MercadoPagoSignatureTestHelper.BuildSignature("payment-feed-transition", "request-feed-approval", "1700000000", Secret),
            "request-feed-approval",
            "payment-feed-transition"), CancellationToken.None);

        gateway.SetPaymentDetails(new MercadoPagoPaymentDetails(
            "payment-feed-transition", hogarId, "cancelled", null, null,
            new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc)));
        var cancellation = await handler.Handle(new ProcessWebhookCommand(
            payload,
            MercadoPagoSignatureTestHelper.BuildSignature("payment-feed-transition", "request-feed-cancellation", "1700000000", Secret),
            "request-feed-cancellation",
            "payment-feed-transition"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Processed, approval.Outcome);
        Assert.Equal(ProcessWebhookOutcome.Processed, cancellation.Outcome);
        Assert.Equal(2, repository.ProcessedEventIds.Count);
    }

    [Fact]
    public async Task Handle_MerchantOrderPayload_ReturnsIgnoredWithoutPaymentLookupOrPlanUpdate()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("merchant-order-1", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = "{\"resource\":\"merchant-order-1\",\"topic\":\"merchant_order\"}";
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("merchant-order-1", "request-merchant-order", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-merchant-order", "merchant-order-1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(0, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
        Assert.Null(repository.LastPlanUpdate);
    }

    [Fact]
    public async Task Handle_V1MerchantOrderPayload_ReturnsIgnoredWithoutPaymentLookupOrPlanUpdate()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("merchant-order-v1", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-merchant-order-v1", "merchant_order", "merchant-order-v1");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("merchant-order-v1", "request-merchant-order-v1", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-merchant-order-v1", "merchant-order-v1"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(0, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
        Assert.Null(repository.LastPlanUpdate);
    }

    [Fact]
    public async Task Handle_PaymentLookupHttpFailure_ReturnsRetryableFailureAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new HttpRequestException("Mercado Pago unavailable", null, HttpStatusCode.InternalServerError));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-3", "payment", "payment-3");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-3", "request-3", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-3", "payment-3"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.RetryableFailure, result.Outcome);
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_MalformedWebhookJson_ReturnsIgnoredAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-malformed", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-malformed", "request-malformed", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand("{ malformed json", signature, "request-malformed", "payment-malformed"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(0, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_WebhookPayloadMissingEnvelopeFields_ReturnsIgnoredAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-missing", Guid.NewGuid(), "approved", null));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-missing", "request-missing", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand("{\"id\":\"event-missing\",\"data\":{}}", signature, "request-missing", "payment-missing"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(0, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_PaymentLookupNotFound_ReturnsIgnoredAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new HttpRequestException("Mercado Pago payment not found", null, HttpStatusCode.NotFound));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-not-found", "payment", "payment-not-found");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-not-found", "request-not-found", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-not-found", "payment-not-found"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_PaymentLookupInvalidMetadata_ReturnsIgnoredAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new InvalidOperationException("Invalid payment metadata: invalid HogarId value."));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-invalid-metadata", "payment", "payment-invalid-metadata");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-invalid-metadata", "request-invalid-metadata", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-invalid-metadata", "payment-invalid-metadata"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_PaymentLookupTimeout_ReturnsRetryableFailureAndDoesNotStoreEvent()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new TaskCanceledException("Mercado Pago timeout"));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-4", "payment", "payment-4");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-4", "request-4", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-4", "payment-4"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.RetryableFailure, result.Outcome);
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_PaymentLookupCanceledByCaller_RethrowsCancellation()
    {
        var repository = new FakePaymentRepository();
        var gateway = new FakeMercadoPagoGateway(new TaskCanceledException("Caller canceled request"));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-canceled", "payment", "payment-canceled");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-canceled", "request-canceled", "1700000000", Secret);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => handler.Handle(new ProcessWebhookCommand(payload, signature, "request-canceled", "payment-canceled"), cts.Token));
        Assert.Equal(1, gateway.PaymentLookupCount);
        Assert.Equal(0, repository.RecordAttempts);
    }

    [Fact]
    public async Task Handle_RepositoryMissingHogar_ReturnsIgnored()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakePaymentRepository
        {
            ProcessWebhookException = new InvalidOperationException($"Household (Hogar) with ID '{hogarId}' was not found.")
        };
        var gateway = new FakeMercadoPagoGateway(new MercadoPagoPaymentDetails("payment-5", hogarId, "approved", null, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)));
        var handler = new ProcessWebhookHandler(repository, gateway, new MercadoPagoWebhookSignatureVerifier(), new MercadoPagoOptions { WebhookSecret = Secret });
        var payload = BuildPayload("event-5", "payment", "payment-5");
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-5", "request-5", "1700000000", Secret);

        var result = await handler.Handle(new ProcessWebhookCommand(payload, signature, "request-5", "payment-5"), CancellationToken.None);

        Assert.Equal(ProcessWebhookOutcome.Ignored, result.Outcome);
        Assert.Equal(1, repository.RecordAttempts);
        Assert.Null(repository.LastPlanUpdate);
    }

    private static string BuildPayload(string eventId, string eventType, string dataId)
        => $"{{\"id\":\"{eventId}\",\"type\":\"{eventType}\",\"data\":{{\"id\":\"{dataId}\"}}}}";

    private sealed class FakeMercadoPagoGateway : IMercadoPagoGateway
    {
        private MercadoPagoPaymentDetails? _paymentDetails;
        private readonly Exception? _exception;

        public FakeMercadoPagoGateway(MercadoPagoPaymentDetails paymentDetails)
        {
            _paymentDetails = paymentDetails;
        }

        public FakeMercadoPagoGateway(Exception exception)
        {
            _exception = exception;
        }

        public int PaymentLookupCount { get; private set; }
        public string? LastPaymentId { get; private set; }

        public Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(MercadoPagoCheckoutPreferenceRequest request, CancellationToken ct)
            => Task.FromResult(new MercadoPagoCheckoutPreference("pref-test", new Uri("https://mp.test/checkout")));

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct)
        {
            PaymentLookupCount++;
            LastPaymentId = paymentId;
            if (_exception is not null)
            {
                return Task.FromException<MercadoPagoPaymentDetails>(_exception);
            }

            return Task.FromResult(_paymentDetails!);
        }

        public void SetPaymentDetails(MercadoPagoPaymentDetails paymentDetails)
            => _paymentDetails = paymentDetails;
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public HashSet<string> DuplicateEventIds { get; } = [];
        public HashSet<string> ProcessedEventIds { get; } = [];
        public int RecordAttempts { get; private set; }
        public PaymentPlanUpdate? LastPlanUpdate { get; private set; }
        public Exception? ProcessWebhookException { get; init; }

        public Task<HouseholdEntitlement> GetSubscriptionAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(new HouseholdEntitlement(HouseholdPlan.Free, SubscriptionStatus.None, null));

        public Task<ProcessWebhookOutcome> ProcessWebhookEventAsync(PaymentWebhookEventRecord webhookEvent, PaymentPlanUpdate planUpdate, CancellationToken ct)
        {
            RecordAttempts++;
            if (ProcessWebhookException is not null)
            {
                return Task.FromException<ProcessWebhookOutcome>(ProcessWebhookException);
            }

            if (DuplicateEventIds.Contains(webhookEvent.ProviderEventId))
            {
                return Task.FromResult(ProcessWebhookOutcome.Duplicate);
            }

            LastPlanUpdate = planUpdate;
            ProcessedEventIds.Add(webhookEvent.ProviderEventId);
            return Task.FromResult(ProcessWebhookOutcome.Processed);
        }
    }
}
