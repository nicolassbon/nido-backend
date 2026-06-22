using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Infrastructure.Telegram.Webhook;

namespace Nido.Api.Middleware;

public sealed class TelegramWebhookPayloadSizeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<TelegramOptions> _options;
    private readonly ITelegramWebhookTelemetry _telemetry;
    private readonly ILogger<TelegramWebhookPayloadSizeMiddleware> _logger;

    public TelegramWebhookPayloadSizeMiddleware(
        RequestDelegate next,
        IOptionsMonitor<TelegramOptions> options,
        ITelegramWebhookTelemetry telemetry,
        ILogger<TelegramWebhookPayloadSizeMiddleware> logger)
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

        var maxBytes = _options.CurrentValue.WebhookMaxPayloadBytes;

        if (context.Request.ContentLength is long length && length > maxBytes)
        {
            RejectOversized(context);
            return;
        }

        var buffered = await BufferBoundedBodyAsync(context.Request.Body, maxBytes, context.RequestAborted);
        if (buffered.Oversized)
        {
            RejectOversized(context);
            await buffered.Stream.DisposeAsync();
            return;
        }

        context.Request.Body = buffered.Stream;
        context.Request.ContentLength = buffered.Stream.Length;

        try
        {
            await _next(context);
        }
        finally
        {
            await buffered.Stream.DisposeAsync();
        }
    }

    private void RejectOversized(HttpContext context)
    {
        var stopwatch = TryGetStopwatch(context);
        stopwatch?.Stop();
        _telemetry.RecordOversized(stopwatch?.Elapsed.TotalMilliseconds ?? 0d);
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
    }

    private static async Task<BoundedBuffer> BufferBoundedBodyAsync(
        Stream body,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var memory = new MemoryStream(capacity: Math.Min(maxBytes, 4096));
        var copyBuffer = new byte[4096];
        var total = 0L;
        var limit = maxBytes + 1L;

        while (total < limit)
        {
            var remaining = (int)Math.Min(copyBuffer.Length, limit - total);
            var read = await body.ReadAsync(copyBuffer.AsMemory(0, remaining), cancellationToken);
            if (read == 0)
            {
                break;
            }

            memory.Write(copyBuffer, 0, read);
            total += read;
        }

        if (total > maxBytes)
        {
            return new BoundedBuffer(memory, Oversized: true);
        }

        memory.Position = 0;
        return new BoundedBuffer(memory, Oversized: false);
    }

    private static bool IsWebhookRoute(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && string.Equals(
                request.Path,
                TelegramWebhookSecretMiddleware.RoutePath,
                StringComparison.OrdinalIgnoreCase);
    }

    private static Stopwatch? TryGetStopwatch(HttpContext context)
    {
        return context.Items.TryGetValue(TelegramWebhookSecretMiddleware.StopwatchItemKey, out var raw)
            ? raw as Stopwatch
            : null;
    }

    private readonly record struct BoundedBuffer(MemoryStream Stream, bool Oversized);
}
