using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Nido.Infrastructure.Telegram.Webhook;

public interface ITelegramWebhookTelemetry
{
    void RecordAccepted(double elapsedMilliseconds);

    void RecordDuplicate(double elapsedMilliseconds);

    void RecordInvalidSecret(double elapsedMilliseconds);

    void RecordOversized(double elapsedMilliseconds);

    void RecordMalformed(double elapsedMilliseconds);

    void RecordThrottled(double elapsedMilliseconds);
}

public sealed class TelegramWebhookTelemetry : ITelegramWebhookTelemetry
{
    public const string MeterName = "Nido.Telegram.Webhook";

    private readonly ILogger<TelegramWebhookTelemetry> _logger;
    private readonly Counter<long> _accepted;
    private readonly Counter<long> _duplicates;
    private readonly Counter<long> _invalidSecret;
    private readonly Counter<long> _oversized;
    private readonly Counter<long> _malformed;
    private readonly Counter<long> _throttled;
    private readonly Histogram<double> _latency;

    public TelegramWebhookTelemetry(ILogger<TelegramWebhookTelemetry> logger)
    {
        _logger = logger;

        var meter = new Meter(MeterName, "1.0.0");
        _accepted = meter.CreateCounter<long>("telegram.webhook.accepted", description: "Accepted Telegram webhook requests.");
        _duplicates = meter.CreateCounter<long>("telegram.webhook.duplicates", description: "Duplicate Telegram updates short-circuited by idempotency.");
        _invalidSecret = meter.CreateCounter<long>("telegram.webhook.rejected.invalid_secret", description: "Webhook requests rejected for missing or wrong secret token.");
        _oversized = meter.CreateCounter<long>("telegram.webhook.rejected.oversized", description: "Webhook requests rejected for exceeding the body size limit.");
        _malformed = meter.CreateCounter<long>("telegram.webhook.rejected.malformed", description: "Webhook requests rejected because the body could not be parsed.");
        _throttled = meter.CreateCounter<long>("telegram.webhook.rejected.throttled", description: "Webhook requests rejected by the route rate limiter.");
        _latency = meter.CreateHistogram<double>("telegram.webhook.latency", unit: "ms", description: "End-to-end processing latency for every Telegram webhook request, regardless of outcome.");
    }

    public void RecordAccepted(double elapsedMilliseconds)
    {
        _accepted.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("accepted", elapsedMilliseconds);
    }

    public void RecordDuplicate(double elapsedMilliseconds)
    {
        _duplicates.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("duplicate", elapsedMilliseconds);
    }

    public void RecordInvalidSecret(double elapsedMilliseconds)
    {
        _invalidSecret.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("rejected.invalid_secret", elapsedMilliseconds);
    }

    public void RecordOversized(double elapsedMilliseconds)
    {
        _oversized.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("rejected.oversized", elapsedMilliseconds);
    }

    public void RecordMalformed(double elapsedMilliseconds)
    {
        _malformed.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("rejected.malformed", elapsedMilliseconds);
    }

    public void RecordThrottled(double elapsedMilliseconds)
    {
        _throttled.Add(1);
        RecordLatency(elapsedMilliseconds);
        EmitOutcome("rejected.throttled", elapsedMilliseconds);
    }

    private void RecordLatency(double elapsedMilliseconds)
    {
        if (elapsedMilliseconds >= 0)
        {
            _latency.Record(elapsedMilliseconds);
        }
    }

    private void EmitOutcome(string outcome, double elapsedMilliseconds)
    {
        _logger.LogInformation(
            "Telegram webhook outcome={Outcome} latency_ms={LatencyMs}",
            outcome,
            elapsedMilliseconds);
    }
}
