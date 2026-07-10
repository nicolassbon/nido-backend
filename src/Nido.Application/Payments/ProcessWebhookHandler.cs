using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Nido.Application.Payments;

public sealed class ProcessWebhookHandler
{
    private const string Provider = "mercadopago";
    private readonly IPaymentRepository _repository;
    private readonly IMercadoPagoGateway _gateway;
    private readonly MercadoPagoWebhookSignatureVerifier _signatureVerifier;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<ProcessWebhookHandler>? _logger;

    public ProcessWebhookHandler(
        IPaymentRepository repository,
        IMercadoPagoGateway gateway,
        MercadoPagoWebhookSignatureVerifier signatureVerifier,
        MercadoPagoOptions options,
        ILogger<ProcessWebhookHandler>? logger = null)
    {
        _repository = repository;
        _gateway = gateway;
        _signatureVerifier = signatureVerifier;
        _options = options;
        _logger = logger;
    }

    public async Task<ProcessWebhookResult> Handle(ProcessWebhookCommand command, CancellationToken ct)
    {
        if (!_signatureVerifier.Verify(command.DataId, command.RequestId, command.Signature, _options.WebhookSecret))
        {
            return new ProcessWebhookResult(ProcessWebhookOutcome.Unauthorized);
        }

        MercadoPagoWebhookEnvelope envelope;
        try
        {
            envelope = MercadoPagoWebhookEnvelope.Parse(command.Payload);
        }
        catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
        {
            _logger?.LogWarning("Ignored Mercado Pago webhook because the envelope could not be parsed. Outcome: {Outcome}", ProcessWebhookOutcome.Ignored);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }

        if (!envelope.IsPaymentNotification)
        {
            _logger?.LogInformation(
                "Ignored non-payment Mercado Pago webhook. Outcome: {Outcome}, EventType: {EventType}",
                ProcessWebhookOutcome.Ignored,
                envelope.EventType);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }

        if (!string.Equals(envelope.DataId, command.DataId, StringComparison.Ordinal))
        {
            _logger?.LogWarning(
                "Ignored Mercado Pago webhook because its unsigned payload payment ID did not match the signed payment ID. Outcome: {Outcome}",
                ProcessWebhookOutcome.Ignored);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }

        MercadoPagoPaymentDetails payment;
        try
        {
            payment = await _gateway.GetPaymentAsync(command.DataId, ct);
        }
        catch (InvalidOperationException ex) when (IsInvalidPaymentMetadataException(ex))
        {
            _logger?.LogWarning(
                "Ignored Mercado Pago webhook due to invalid payment metadata. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}",
                ProcessWebhookOutcome.Ignored,
                envelope.EventType,
                envelope.DataId);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger?.LogInformation(
                "Ignored Mercado Pago webhook because the payment was not found. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}, ProviderStatusCode: {ProviderStatusCode}",
                ProcessWebhookOutcome.Ignored,
                envelope.EventType,
                envelope.DataId,
                ex.StatusCode);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(
                "Mercado Pago payment lookup failed and the webhook should be retried. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}, ProviderStatusCode: {ProviderStatusCode}",
                ProcessWebhookOutcome.RetryableFailure,
                envelope.EventType,
                envelope.DataId,
                ex.StatusCode);
            return new ProcessWebhookResult(ProcessWebhookOutcome.RetryableFailure);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            _logger?.LogInformation(
                "Mercado Pago payment lookup was canceled by the caller. EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}",
                envelope.EventType,
                envelope.DataId);
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger?.LogWarning(
                "Mercado Pago payment lookup timed out and the webhook should be retried. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}",
                ProcessWebhookOutcome.RetryableFailure,
                envelope.EventType,
                envelope.DataId);
            return new ProcessWebhookResult(ProcessWebhookOutcome.RetryableFailure);
        }

        if ((IsActiveStatus(payment.Status) && payment.DateApproved is null)
            || payment.ProviderTransitionAt is null)
        {
            _logger?.LogWarning(
                "Ignored Mercado Pago payment because it did not include the required stable provider transition timestamp. Outcome: {Outcome}",
                ProcessWebhookOutcome.Ignored);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }

        var update = ResolvePlanUpdate(payment);
        var providerEventId = envelope.EventId ?? CreateLegacyEventId(envelope.EventType, update, payment.Status);

        ProcessWebhookOutcome outcome;
        try
        {
            outcome = await _repository.ProcessWebhookEventAsync(
                new PaymentWebhookEventRecord(
                    Provider,
                    providerEventId,
                    update.ProviderPaymentId,
                    update.ProviderSubscriptionId,
                    envelope.EventType,
                    command.Payload,
                    payment.HogarId),
                update,
                ct);
        }
        catch (InvalidOperationException)
        {
            _logger?.LogWarning(
                "Ignored Mercado Pago webhook because the payment update could not be applied. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}",
                ProcessWebhookOutcome.Ignored,
                envelope.EventType,
                update.ProviderPaymentId);
            return new ProcessWebhookResult(ProcessWebhookOutcome.Ignored);
        }

        _logger?.LogInformation(
            "Handled Mercado Pago webhook. Outcome: {Outcome}, EventType: {EventType}, ProviderPaymentId: {ProviderPaymentId}",
            outcome,
            envelope.EventType,
            update.ProviderPaymentId);

        return new ProcessWebhookResult(outcome);
    }

    private PaymentPlanUpdate ResolvePlanUpdate(MercadoPagoPaymentDetails payment)
    {
        var status = payment.Status.ToLowerInvariant() switch
        {
            "approved" or "active" or "trialing" => SubscriptionStatus.Active,
            "cancelled" or "canceled" => SubscriptionStatus.Cancelled,
            "past_due" => SubscriptionStatus.PastDue,
            _ => SubscriptionStatus.Pending
        };

        var plan = status == SubscriptionStatus.Active
            ? HouseholdPlan.Premium
            : HouseholdPlan.Free;

        DateTime? subscriptionEndsAt = null;
        if (status == SubscriptionStatus.Active)
        {
            subscriptionEndsAt = payment.DateApproved!.Value.AddDays(30);
        }

        return new PaymentPlanUpdate(
            payment.HogarId,
            plan,
            status,
            payment.ProviderPaymentId,
            payment.ProviderSubscriptionId,
            subscriptionEndsAt,
            payment.ProviderTransitionAt!.Value);
    }

    private static bool IsActiveStatus(string status)
        => status.Equals("approved", StringComparison.OrdinalIgnoreCase)
           || status.Equals("active", StringComparison.OrdinalIgnoreCase)
           || status.Equals("trialing", StringComparison.OrdinalIgnoreCase);

    private static string CreateLegacyEventId(string eventType, PaymentPlanUpdate update, string paymentStatus)
    {
        var canonicalIdentity = string.Join('|',
            eventType.Trim().ToLowerInvariant(),
            update.ProviderPaymentId?.Trim() ?? string.Empty,
            update.ProviderTransitionAt.ToUniversalTime().ToString("O"),
            paymentStatus.Trim().ToLowerInvariant());
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalIdentity));
        return $"legacy:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static bool IsInvalidPaymentMetadataException(InvalidOperationException ex)
        => ex.Message.Contains("Invalid HogarId", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("invalid hogar", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("invalid payment metadata", StringComparison.OrdinalIgnoreCase);

    private sealed record MercadoPagoWebhookEnvelope(string? EventId, string EventType, string DataId, bool IsPaymentNotification)
    {
        public static MercadoPagoWebhookEnvelope Parse(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (TryGetNonBlankString(root, "resource", out var resource)
                && TryGetNonBlankString(root, "topic", out var topic))
            {
                var eventId = TryGetNonBlankString(root, "id", out var feedEventId)
                    ? feedEventId
                    : null;

                var dataId = LastResourceSegment(resource);

                return new MercadoPagoWebhookEnvelope(
                    eventId,
                    topic,
                    dataId,
                    topic.Equals("payment", StringComparison.OrdinalIgnoreCase));
            }

            var v1EventId = GetRequiredString(root, "id");
            var eventType = GetRequiredString(root, "type");
            var dataIdFromV1 = GetRequiredString(root.GetProperty("data"), "id");
            return new MercadoPagoWebhookEnvelope(
                v1EventId,
                eventType,
                dataIdFromV1,
                eventType.Equals("payment", StringComparison.OrdinalIgnoreCase));
        }

        private static string LastResourceSegment(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                return string.Empty;
            }

            var trimmed = resource.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash < 0 ? trimmed : trimmed[(lastSlash + 1)..];
        }

        private static string GetRequiredString(JsonElement element, string propertyName)
        {
            if (!TryGetNonBlankString(element, propertyName, out var value))
            {
                throw new InvalidOperationException($"Webhook envelope field '{propertyName}' is required.");
            }

            return value;
        }

        private static bool TryGetNonBlankString(JsonElement element, string propertyName, out string value)
        {
            value = string.Empty;
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString() ?? string.Empty,
                JsonValueKind.Number => property.GetRawText(),
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(value);
        }
    }
}

public sealed record ProcessWebhookCommand(
    string Payload,
    string Signature,
    string RequestId,
    string DataId = "");

public sealed record ProcessWebhookResult(ProcessWebhookOutcome Outcome);

public enum ProcessWebhookOutcome
{
    Processed,
    Duplicate,
    Unauthorized,
    Ignored,
    RetryableFailure
}
