using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Infrastructure.Telegram;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramWebhookInitializerTests
{
    [Fact]
    public async Task InitializeAsync_SendsSetWebhookWithUrlSecretAndMessageUpdates()
    {
        HttpRequestMessage? capturedRequest = null;

        var handler = new TestHandler(async (request, ct) =>
        {
            capturedRequest = request;
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal("https://telegram-webhook.example.test/api/webhooks/telegram", root.GetProperty("url").GetString());
            Assert.Equal("secret-token", root.GetProperty("secret_token").GetString());

            var allowedUpdates = root.GetProperty("allowed_updates")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToArray();

            Assert.Equal(new[] { "message" }, allowedUpdates);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true,"result":true,"description":"Webhook is set"}""")
            };
        });

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/botmy-token/")
        };

        var options = Options.Create(new TelegramOptions
        {
            BotToken = "my-token",
            WebhookSecretToken = "secret-token",
            WebhookUrl = "https://telegram-webhook.example.test/api/webhooks/telegram"
        });

        var initializer = new TelegramWebhookInitializer(
            http,
            options,
            NullLogger<TelegramWebhookInitializer>.Instance);

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.EndsWith("/setWebhook", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task InitializeAsync_Throws_WhenTelegramReturnsError()
    {
        var handler = new TestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"ok":false,"error_code":400,"description":"Bad Request: invalid url"}""")
        }));

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/botmy-token/")
        };

        var options = Options.Create(new TelegramOptions
        {
            BotToken = "my-token",
            WebhookSecretToken = "secret-token",
            WebhookUrl = "https://telegram-webhook.example.test/api/webhooks/telegram"
        });

        var initializer = new TelegramWebhookInitializer(
            http,
            options,
            NullLogger<TelegramWebhookInitializer>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.InitializeAsync(CancellationToken.None));

        Assert.Contains("setWebhook failed", ex.Message);
    }

    [Fact]
    public async Task InitializeAsync_Throws_OnNetworkFailure()
    {
        var handler = new TestHandler((_, _) => throw new HttpRequestException("No connection"));

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/botmy-token/")
        };

        var options = Options.Create(new TelegramOptions
        {
            BotToken = "my-token",
            WebhookSecretToken = "secret-token",
            WebhookUrl = "https://telegram-webhook.example.test/api/webhooks/telegram"
        });

        var initializer = new TelegramWebhookInitializer(
            http,
            options,
            NullLogger<TelegramWebhookInitializer>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => initializer.InitializeAsync(CancellationToken.None));
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _callback;

        public TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
        {
            _callback = callback;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _callback(request, cancellationToken);
        }
    }
}
