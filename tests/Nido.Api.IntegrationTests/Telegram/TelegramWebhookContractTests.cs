using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Telegram.Webhook;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Api.IntegrationTests.Telegram;

[Collection("TelegramWebhook")]
public sealed class TelegramWebhookContractTests : IClassFixture<NidoTestWebAppFactory>
{
    private const string Secret = "default-test-webhook-secret";
    private const string Endpoint = "/api/webhooks/telegram";

    private readonly NidoTestWebAppFactory _baseFactory;

    public TelegramWebhookContractTests(NidoTestWebAppFactory baseFactory)
    {
        _baseFactory = baseFactory.WithTelegramWebhookConfig(Secret);
    }

    [Fact]
    public async Task InvalidSecret_DoesNotConsumeRateLimitQuota()
    {
        const int permitPerWindow = 2;
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory
            .WithLogCapture(logCapture)
            .WithTelegramWebhookConfig(
                secret: Secret,
                maxPayloadBytes: 65_536,
                rateLimitPermitPerWindow: permitPerWindow,
                rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var wrong = await PostWrongSecretAsync(client, i);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var first = await PostUpdateAsync(client, 700_000L + 1);
        var second = await PostUpdateAsync(client, 700_000L + 2);
        var third = await PostUpdateAsync(client, 700_000L + 3);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);

        await AssertNoProcessedRowAsync(factory, 0);
        await AssertNoProcessedRowAsync(factory, 4);
    }

    [Fact]
    public async Task OversizedBody_DoesNotConsumeRateLimitQuota()
    {
        const int maxBytes = 4 * 1024;
        const int permitPerWindow = 2;
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory
            .WithLogCapture(logCapture)
            .WithTelegramWebhookConfig(
                secret: Secret,
                maxPayloadBytes: maxBytes,
                rateLimitPermitPerWindow: permitPerWindow,
                rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var oversized = await PostOversizedAsync(client, updateId: 600_000L + i, paddingKilobytes: 16);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
        }

        var first = await PostUpdateAsync(client, 600_100L);
        var second = await PostUpdateAsync(client, 600_101L);
        var third = await PostUpdateAsync(client, 600_102L);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);

        await AssertNoProcessedRowAsync(factory, 600_000L);
        await AssertNoProcessedRowAsync(factory, 600_002L);
    }

    [Fact]
    public async Task ChunkedOversizedBody_Returns413_EvenWithoutContentLength()
    {
        const int maxBytes = 4 * 1024;
        const int permitPerWindow = 2;
        const int oversizedKilobytes = 16;
        using var factory = _baseFactory.WithTelegramWebhookConfig(
            secret: Secret,
            maxPayloadBytes: maxBytes,
            rateLimitPermitPerWindow: permitPerWindow,
            rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        var response = await PostChunkedOversizedAsync(
            client,
            updateId: 600_200L,
            paddingKilobytes: oversizedKilobytes);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        var first = await PostUpdateAsync(client, 600_201L);
        var second = await PostUpdateAsync(client, 600_202L);
        var third = await PostUpdateAsync(client, 600_203L);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);

        await AssertNoProcessedRowAsync(factory, 600_200L);
    }

    [Fact]
    public async Task ChunkedBody_UnderLimit_IsAcceptedAndPersistsRow()
    {
        const int maxBytes = 4 * 1024;
        const long updateId = 600_300L;
        using var factory = _baseFactory.WithTelegramWebhookConfig(Secret, maxPayloadBytes: maxBytes);
        using var client = factory.CreateClient();

        var response = await PostChunkedUpdateAsync(client, updateId, paddingBytes: 256);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertProcessedRowAsync(factory, updateId);
    }

    [Fact]
    public async Task MalformedJson_DoesNotWriteIdempotencyRow()
    {
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory.WithLogCapture(logCapture);
        using var client = factory.CreateClient();

        var countBefore = await CountProcessedRowsAsync(factory);

        using var content = new StringContent("{not-json", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var countAfter = await CountProcessedRowsAsync(factory);
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task MalformedJson_LogsExactlyOneRejectedMalformedOutcome()
    {
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory.WithLogCapture(logCapture);
        using var client = factory.CreateClient();

        using var content = new StringContent("not-valid-json", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var outcomeLogs = logCapture.Entries
            .Where(e => e.Message.Contains("Telegram webhook outcome="))
            .ToList();
        Assert.Single(outcomeLogs);
        Assert.Contains("outcome=rejected.malformed", outcomeLogs[0].Message);
        Assert.DoesNotContain("update_id", outcomeLogs[0].Message);
    }

    [Fact]
    public async Task InvalidSecret_LogsExactlyOneRejectedInvalidSecretOutcome_WithoutEchoingHeader()
    {
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory.WithLogCapture(logCapture);
        using var client = factory.CreateClient();

        const string wrongSecret = "definitely-wrong-secret";
        using var content = new StringContent(BuildUpdateBody(500_001L), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", wrongSecret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var outcomeLogs = logCapture.Entries
            .Where(e => e.Message.Contains("Telegram webhook outcome="))
            .ToList();
        Assert.Single(outcomeLogs);
        Assert.Contains("outcome=rejected.invalid_secret", outcomeLogs[0].Message);

        Assert.DoesNotContain(wrongSecret, outcomeLogs[0].Message);
        Assert.DoesNotContain("500001", outcomeLogs[0].Message);
        Assert.DoesNotContain("update_id", outcomeLogs[0].Message);
    }

    [Fact]
    public async Task OversizedBody_LogsExactlyOneRejectedOversizedOutcome()
    {
        const int maxBytes = 4 * 1024;
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory
            .WithLogCapture(logCapture)
            .WithTelegramWebhookConfig(Secret, maxPayloadBytes: maxBytes);
        using var client = factory.CreateClient();

        var response = await PostOversizedAsync(client, updateId: 500_002L, paddingKilobytes: 16);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        var outcomeLogs = logCapture.Entries
            .Where(e => e.Message.Contains("Telegram webhook outcome="))
            .ToList();
        Assert.Single(outcomeLogs);
        Assert.Contains("outcome=rejected.oversized", outcomeLogs[0].Message);
        Assert.DoesNotContain("update_id", outcomeLogs[0].Message);
    }

    [Fact]
    public async Task ThrottledRequest_LogsExactlyOneRejectedThrottledOutcome()
    {
        const int permitPerWindow = 1;
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory
            .WithLogCapture(logCapture)
            .WithTelegramWebhookConfig(
                secret: Secret,
                maxPayloadBytes: 65_536,
                rateLimitPermitPerWindow: permitPerWindow,
                rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        var first = await PostUpdateAsync(client, 500_010L);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await PostUpdateAsync(client, 500_011L);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);

        var throttledLogs = logCapture.Entries
            .Where(e => e.Message.Contains("outcome=rejected.throttled"))
            .ToList();
        Assert.Single(throttledLogs);
    }

    [Fact]
    public async Task AcceptedRequest_LogsExactlyOneAcceptedOutcome_WithoutUpdateId()
    {
        const long updateId = 500_020L;
        var logCapture = new TestLogCapture();
        using var factory = _baseFactory.WithLogCapture(logCapture);
        using var client = factory.CreateClient();

        var response = await PostUpdateAsync(client, updateId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var outcomeLogs = logCapture.Entries
            .Where(e => e.Message.Contains("Telegram webhook outcome="))
            .ToList();
        Assert.Single(outcomeLogs);
        Assert.Contains("outcome=accepted", outcomeLogs[0].Message);
        Assert.DoesNotContain("update_id", outcomeLogs[0].Message);
        Assert.DoesNotContain(updateId.ToString(), outcomeLogs[0].Message);
    }

    [Fact]
    public async Task ConfigurablePayloadLimit_IsHonored()
    {
        const int customLimit = 32 * 1024;
        using var factory = _baseFactory.WithTelegramWebhookConfig(Secret, maxPayloadBytes: customLimit);
        using var client = factory.CreateClient();

        var okBody = "{\"update_id\":" + 500_030L + ",\"message\":{\"text\":\""
                     + new string('x', customLimit - 100) + "\"}}";
        using var okContent = new StringContent(okBody, Encoding.UTF8, "application/json");
        var okRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = okContent };
        okRequest.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        var okResponse = await client.SendAsync(okRequest);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        var oversized = await PostOversizedAsync(client, updateId: 500_031L, paddingKilobytes: 64);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
    }

    [Fact]
    public async Task EveryOutcome_RecordsLatencyOnTheHistogram()
    {
        const int permitPerWindow = 2;
        const int maxBytes = 4 * 1024;
        using var listener = new MeterListener();
        var latencyValues = new System.Collections.Concurrent.ConcurrentBag<double>();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == TelegramWebhookTelemetry.MeterName
                && instrument.Name == "telegram.webhook.latency")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (instrument.Meter.Name == TelegramWebhookTelemetry.MeterName
                && instrument.Name == "telegram.webhook.latency")
            {
                latencyValues.Add(value);
            }
        });
        listener.Start();

        using var factory = _baseFactory.WithTelegramWebhookConfig(
            secret: Secret,
            maxPayloadBytes: maxBytes,
            rateLimitPermitPerWindow: permitPerWindow,
            rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        await PostWrongSecretAsync(client, 0);
        await PostOversizedAsync(client, updateId: 1, paddingKilobytes: 16);

        using (var bad = new StringContent("not-valid-json", Encoding.UTF8, "application/json"))
        {
            var badRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = bad };
            badRequest.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
            await client.SendAsync(badRequest);
        }

        var accepted = await PostUpdateAsync(client, 2);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var throttled = await PostUpdateAsync(client, 3);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        listener.RecordObservableInstruments();

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (latencyValues.Count < 5 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Equal(5, latencyValues.Count);
    }

    private static async Task<HttpResponseMessage> PostUpdateAsync(HttpClient client, long updateId)
    {
        using var content = new StringContent(BuildUpdateBody(updateId), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostWrongSecretAsync(HttpClient client, long updateId)
    {
        using var content = new StringContent(BuildUpdateBody(updateId), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "wrong-secret-" + updateId);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostOversizedAsync(HttpClient client, long updateId, int paddingKilobytes)
    {
        var oversized = new string('a', paddingKilobytes * 1024);
        using var content = new StringContent(
            "{\"update_id\":" + updateId + ",\"message\":{\"text\":\"" + oversized + "\"}}",
            Encoding.UTF8,
            "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostChunkedOversizedAsync(
        HttpClient client,
        long updateId,
        int paddingKilobytes)
    {
        var padding = new string('a', paddingKilobytes * 1024);
        var bodyBytes = Encoding.UTF8.GetBytes(
            "{\"update_id\":" + updateId + ",\"message\":{\"text\":\"" + padding + "\"}}");
        var bodyStream = new MemoryStream(bodyBytes, writable: false);

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StreamContent(bodyStream)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            }
        };
        request.Headers.TransferEncodingChunked = true;
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostChunkedUpdateAsync(
        HttpClient client,
        long updateId,
        int paddingBytes)
    {
        var padding = new string('b', paddingBytes);
        var bodyBytes = Encoding.UTF8.GetBytes(
            "{\"update_id\":" + updateId + ",\"message\":{\"message_id\":1,\"date\":1,\"text\":\"" + padding + "\",\"chat\":{\"id\":1,\"type\":\"private\"}}}");
        var bodyStream = new MemoryStream(bodyBytes, writable: false);

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StreamContent(bodyStream)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            }
        };
        request.Headers.TransferEncodingChunked = true;
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static string BuildUpdateBody(long updateId)
        => "{\"update_id\":" + updateId + ",\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}";

    private static async Task AssertNoProcessedRowAsync(NidoTestWebAppFactory factory, long updateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.False(exists, $"Expected no row in processed_telegram_updates for update_id={updateId}.");
    }

    private static async Task AssertProcessedRowAsync(NidoTestWebAppFactory factory, long updateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.True(exists, $"Expected a row in processed_telegram_updates for update_id={updateId}.");
    }

    private static async Task<int> CountProcessedRowsAsync(NidoTestWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        return await db.ProcessedTelegramUpdates.CountAsync();
    }
}
