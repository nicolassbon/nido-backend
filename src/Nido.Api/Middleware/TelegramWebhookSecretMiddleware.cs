using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Infrastructure.Telegram.Webhook;

namespace Nido.Api.Middleware;

public sealed class TelegramWebhookSecretMiddleware
{
    public const string HeaderName = "X-Telegram-Bot-Api-Secret-Token";
    public const string RoutePath = "/api/webhooks/telegram";
    public const string StopwatchItemKey = "Nido.Telegram.Webhook.Stopwatch";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<TelegramOptions> _options;
    private readonly ITelegramWebhookTelemetry _telemetry;
    private readonly ILogger<TelegramWebhookSecretMiddleware> _logger;

    public TelegramWebhookSecretMiddleware(
        RequestDelegate next,
        IOptionsMonitor<TelegramOptions> options,
        ITelegramWebhookTelemetry telemetry,
        ILogger<TelegramWebhookSecretMiddleware> logger)
    {
        _next = next;
        _options = options;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsWebhookRoute(context.Request))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        context.Items[StopwatchItemKey] = stopwatch;

        var expected = _options.CurrentValue.WebhookSecretToken;
        if (string.IsNullOrEmpty(expected))
        {
            stopwatch.Stop();
            _telemetry.RecordInvalidSecret(stopwatch.Elapsed.TotalMilliseconds);
            await WriteUnauthorizedAsync(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var supplied) || string.IsNullOrEmpty(supplied))
        {
            stopwatch.Stop();
            _telemetry.RecordInvalidSecret(stopwatch.Elapsed.TotalMilliseconds);
            await WriteUnauthorizedAsync(context);
            return;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(supplied.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        if (suppliedBytes.Length != expectedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes))
        {
            stopwatch.Stop();
            _telemetry.RecordInvalidSecret(stopwatch.Elapsed.TotalMilliseconds);
            await WriteUnauthorizedAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsWebhookRoute(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && string.Equals(request.Path, RoutePath, StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
