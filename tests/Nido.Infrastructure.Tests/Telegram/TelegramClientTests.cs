using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Telegram.Client;
using Nido.Infrastructure.Telegram;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramClientTests
{
    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/botTEST_TOKEN/")
        };
    }

    [Fact]
    public async Task SendMessageAsync_Success_ReturnsMessageId()
    {
        var handler = new FakeHttpMessageHandler((req, _) =>
        {
            Assert.Equal("POST", req.Method.Method);
            Assert.EndsWith("/sendMessage", req.RequestUri?.ToString());

            var response = new
            {
                ok = true,
                result = new { message_id = 12345L }
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var success = Assert.IsType<TelegramSendResult.Success>(result);
        Assert.Equal(12345L, success.Message.MessageId);
    }

    [Fact]
    public async Task SendMessageAsync_WithParseModeAndReplyMarkup_PassesValues()
    {
        var handler = new FakeHttpMessageHandler(async (req, _) =>
        {
            var body = await req.Content!.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.Equal("MarkdownV2", root.GetProperty("parse_mode").GetString());
            var keyboard = root.GetProperty("reply_markup").GetProperty("inline_keyboard");
            Assert.Equal(1, keyboard.GetArrayLength());
            Assert.Equal(1, keyboard[0].GetArrayLength());
            Assert.Equal("Click", keyboard[0][0].GetProperty("text").GetString());
            Assert.Equal("click_1", keyboard[0][0].GetProperty("callback_data").GetString());

            var response = new
            {
                ok = true,
                result = new { message_id = 999L }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var markup = new TelegramInlineKeyboardMarkup(new[]
        {
            new[] { new TelegramInlineKeyboardButton("Click", "click_1") }
        });

        var result = await client.SendMessageAsync(42L, "Hello", "MarkdownV2", markup);

        var success = Assert.IsType<TelegramSendResult.Success>(result);
        Assert.Equal(999L, success.Message.MessageId);
    }

    [Fact]
    public async Task SendMessageAsync_RateLimit429_ReturnsRetryAfter()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 429,
                description = "Too Many Requests: retry after 15",
                parameters = new { retry_after = 15 }
            };

            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var rateLimit = Assert.IsType<TelegramRateLimitError>(error.Value);
        Assert.Equal(15, rateLimit.RetryAfter);
        Assert.Equal("TELEGRAM_RATE_LIMIT", rateLimit.Code);
    }

    [Fact]
    public async Task SendMessageAsync_Forbidden403_ReturnsPermanentError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 403,
                description = "Forbidden: bot was blocked by the user"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var permanent = Assert.IsType<TelegramPermanentError>(error.Value);
        Assert.Equal("TELEGRAM_PERMANENT_ERROR", permanent.Code);
        Assert.Contains("blocked", permanent.Description);
    }

    [Fact]
    public async Task SendMessageAsync_Unauthorized401_ReturnsPermanentError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 401,
                description = "Unauthorized"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var permanent = Assert.IsType<TelegramPermanentError>(error.Value);
        Assert.Equal("TELEGRAM_PERMANENT_ERROR", permanent.Code);
    }

    [Fact]
    public async Task SendMessageAsync_BadRequest400_ReturnsPermanentError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 400,
                description = "Bad Request: chat not found"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        Assert.IsType<TelegramPermanentError>(error.Value);
    }

    [Fact]
    public async Task SendMessageAsync_ServerError500_ReturnsTransientError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 500,
                description = "Internal Server Error"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        Assert.IsType<TelegramTransientError>(error.Value);
    }

    [Fact]
    public async Task SendMessageAsync_HttpClientTimeout_ReturnsTransientError()
    {
        // Simulates HttpClient timeout: TaskCanceledException with a token
        // that does NOT match the caller's cancellation token.
        var handler = new FakeHttpMessageHandler((_, ct) =>
        {
            using var otherCts = new CancellationTokenSource();
            throw new TaskCanceledException("Timeout", innerException: null, otherCts.Token);
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Contains("timed out", transient.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessageAsync_CallerCancellation_ReturnsTransientError()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new FakeHttpMessageHandler((_, ct) =>
        {
            throw new OperationCanceledException(ct);
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello", ct: cts.Token);

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Contains("cancelled", transient.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessageAsync_NetworkError_ReturnsTransientError()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("No connection"));

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Contains("No connection", transient.Description);
    }

    [Fact]
    public async Task SendMessageAsync_InvalidChatId_ReturnsValidationError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Should not reach HTTP layer"));

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(0L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        Assert.IsType<TelegramValidationError>(error.Value);
    }

    [Fact]
    public async Task SendMessageAsync_NegativeChatId_AllowsRequest()
    {
        var handler = new FakeHttpMessageHandler((req, _) =>
        {
            var response = new
            {
                ok = true,
                result = new { message_id = 555L }
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(-1001234567890L, "Hello");

        var success = Assert.IsType<TelegramSendResult.Success>(result);
        Assert.Equal(555L, success.Message.MessageId);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyText_ReturnsValidationError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Should not reach HTTP layer"));

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "   ");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        Assert.IsType<TelegramValidationError>(error.Value);
    }

    [Fact]
    public async Task SendMessageAsync_429WithoutRetryAfter_ReturnsZeroRetryAfter()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new
            {
                ok = false,
                error_code = 429,
                description = "Too Many Requests"
            };

            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var rateLimit = Assert.IsType<TelegramRateLimitError>(error.Value);
        Assert.Equal(0, rateLimit.RetryAfter);
    }

    [Fact]
    public async Task SendMessageAsync_EmptySuccessResponse_ReturnsTransientError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Contains("Failed to parse response", transient.Description);
    }

    [Fact]
    public async Task SendMessageAsync_MalformedErrorResponse_ReturnsTransientError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("not json")
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Contains("Failed to parse response", transient.Description);
    }

    [Fact]
    public async Task SendMessageAsync_UnexpectedSuccessPayload_ReturnsTransientError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new { ok = true, result = new { } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
        });

        var client = new TelegramClient(CreateClient(handler), NullLogger<TelegramClient>.Instance);
        var result = await client.SendMessageAsync(42L, "Hello");

        var error = Assert.IsType<TelegramSendResult.Error>(result);
        var transient = Assert.IsType<TelegramTransientError>(error.Value);
        Assert.Equal("Unexpected response payload from Telegram.", transient.Description);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = (req, _) => handler(req);
        }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
