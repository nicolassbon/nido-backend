using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;
using Xunit;

namespace Nido.Api.IntegrationTests.Telegram;

[Collection("TelegramWebhook")]
public sealed class TelegramWebhookEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private const string Secret = "default-test-webhook-secret";
    private const string Endpoint = "/api/webhooks/telegram";
    private const long AcceptedUpdateId = 11_001L;
    private const long DuplicateUpdateId = 11_002L;

    private readonly NidoTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public TelegramWebhookEndpointTests(NidoTestWebAppFactory baseFactory)
    {
        _factory = baseFactory.WithTelegramWebhookConfig(Secret);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithoutSecretHeader_Returns401()
    {
        var response = await _client.PostAsync(Endpoint, BuildUpdate(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoProcessedRowAsync(1);
    }

    [Fact]
    public async Task Post_WithWrongSecret_Returns401_WithoutDeserializingBody()
    {
        using var content = new StringContent(BuildUpdateBody(2), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "definitely-wrong");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoProcessedRowAsync(2);
    }

    [Fact]
    public async Task Post_WithCorrectSecret_AndNewUpdateId_Returns200_AndPersistsRow()
    {
        var response = await PostUpdateAsync(AcceptedUpdateId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertProcessedRowAsync(AcceptedUpdateId);
    }

    [Fact]
    public async Task Post_WithDuplicateUpdateId_Returns200_WithoutInsertingAgain()
    {
        var first = await PostUpdateAsync(DuplicateUpdateId);
        var second = await PostUpdateAsync(DuplicateUpdateId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await AssertExactlyOneRowAsync(DuplicateUpdateId);
    }

    [Theory]
    [InlineData("{\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}")]
    [InlineData("{\"update_id\":0,\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}")]
    public async Task Post_WithFunctionallyInvalidPayload_Returns400_AndDoesNotPersist(string body)
    {
        var countBefore = await CountProcessedRowsAsync(_factory);
        var response = await PostRawBodyAsync(_client, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var countAfter = await CountProcessedRowsAsync(_factory);
        Assert.Equal(countBefore, countAfter);
        await AssertNoProcessedRowAsync(0);
    }

    [Fact]
    public async Task Post_WithMalformedJson_Returns400_WithoutWritingRow()
    {
        using var content = new StringContent("{not-json", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithOversizedBody_Returns413_BeforeRateLimitConsumption()
    {
        const int maxBytes = 8 * 1024;
        using var factory = _factory.WithTelegramWebhookConfig(Secret, maxPayloadBytes: maxBytes);
        using var client = factory.CreateClient();

        var oversized = new string('a', 70 * 1024);
        using var content = new StringContent(
            "{\"update_id\":99,\"message\":{\"text\":\"" + oversized + "\"}}",
            Encoding.UTF8,
            "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        await AssertNoProcessedRowAsync(factory, 99);
    }

    [Fact]
    public async Task Post_OverRateLimit_Returns429_AndDoesNotPersist()
    {
        using var factory = _factory.WithTelegramWebhookConfig(
            secret: Secret,
            maxPayloadBytes: 65_536,
            rateLimitPermitPerWindow: 3,
            rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var ok = await PostUpdateAsync(client, 8000L + i);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var blocked = await PostUpdateAsync(client, 8003L);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.TryGetValues("Retry-After", out var retryAfterValues));
        Assert.Equal("60", Assert.Single(retryAfterValues));

        await AssertNoProcessedRowAsync(factory, 8003);
    }

    private Task<HttpResponseMessage> PostUpdateAsync(long updateId)
        => PostUpdateAsync(_client, updateId);

    private static async Task<HttpResponseMessage> PostUpdateAsync(HttpClient client, long updateId)
    {
        using var content = BuildUpdate(updateId);
        return await PostAsync(client, content);
    }

    private static async Task<HttpResponseMessage> PostRawBodyAsync(HttpClient client, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await PostAsync(client, content);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static StringContent BuildUpdate(long updateId)
        => new(BuildUpdateBody(updateId), Encoding.UTF8, "application/json");

    private static string BuildUpdateBody(long updateId)
        => "{\"update_id\":" + updateId + ",\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}";

    private async Task AssertNoProcessedRowAsync(long updateId)
        => await AssertNoProcessedRowAsync(_factory, updateId);

    private static async Task AssertNoProcessedRowAsync(NidoTestWebAppFactory factory, long updateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.False(exists, $"Expected no row in processed_telegram_updates for update_id={updateId}.");
    }

    private static async Task<int> CountProcessedRowsAsync(NidoTestWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        return await db.ProcessedTelegramUpdates.CountAsync();
    }

    private async Task AssertProcessedRowAsync(long updateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.True(exists, $"Expected row in processed_telegram_updates for update_id={updateId}.");
    }

    private async Task AssertExactlyOneRowAsync(long updateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var count = await db.ProcessedTelegramUpdates.AsNoTracking()
            .CountAsync(p => p.UpdateId == updateId);
        Assert.Equal(1, count);
    }
}
