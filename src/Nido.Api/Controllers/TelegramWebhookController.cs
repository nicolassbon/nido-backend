using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Nido.Api.Middleware;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Formatting;
using Nido.Application.Telegram.Messaging;
using Nido.Application.Telegram.Webhook;
using Nido.Infrastructure.Telegram.Webhook;

namespace Nido.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    public const string RateLimitPolicyName = "TelegramWebhook";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ITelegramWebhookHandler _handler;
    private readonly ITelegramWebhookTelemetry _telemetry;
    private readonly TelegramUpdateDispatcher _dispatcher;
    private readonly ITelegramOutboxWriter _outboxWriter;
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramWebhookHandler handler,
        ITelegramWebhookTelemetry telemetry,
        TelegramUpdateDispatcher dispatcher,
        ITelegramOutboxWriter outboxWriter,
        TelegramOptions telegramOptions,
        ILogger<TelegramWebhookController> logger)
    {
        _handler = handler;
        _telemetry = telemetry;
        _dispatcher = dispatcher;
        _outboxWriter = outboxWriter;
        _telegramOptions = telegramOptions;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyName)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        TelegramWebhookRequest? payload;
        try
        {
            await using var stream = Request.Body;
            payload = await JsonSerializer.DeserializeAsync<TelegramWebhookRequest>(stream, JsonOptions, ct);
        }
        catch (JsonException)
        {
            RecordMalformed();
            return BadRequest();
        }

        if (payload is null || payload.UpdateId == 0)
        {
            RecordMalformed();
            return BadRequest();
        }

        var result = await _handler.HandleAsync(
            payload,
            async innerCt =>
            {
                var dispatchResult = await _dispatcher.DispatchAsync(payload, innerCt);
                await EnqueueConfirmationAsync(dispatchResult, innerCt);
            },
            ct);

        var sw = TryGetStopwatch();
        sw?.Stop();
        var elapsed = sw?.Elapsed.TotalMilliseconds ?? 0d;

        switch (result)
        {
            case TelegramWebhookResult.Accepted:
                _telemetry.RecordAccepted(elapsed);
                return Ok();
            case TelegramWebhookResult.Duplicate:
                _telemetry.RecordDuplicate(elapsed);
                return Ok();
            case TelegramWebhookResult.Rejected:
                return BadRequest();
            default:
                return BadRequest();
        }
    }

    private void RecordMalformed()
    {
        var stopwatch = TryGetStopwatch();
        stopwatch?.Stop();
        _telemetry.RecordMalformed(stopwatch?.Elapsed.TotalMilliseconds ?? 0d);
    }

    private Stopwatch? TryGetStopwatch()
    {
        return HttpContext.Items.TryGetValue(TelegramWebhookSecretMiddleware.StopwatchItemKey, out var raw)
            ? raw as Stopwatch
            : null;
    }

    private async Task EnqueueConfirmationAsync(TelegramDispatchResult? dispatchResult, CancellationToken ct)
    {
        if (dispatchResult is null)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new TelegramOutboxPayload(
            MarkdownV2Escaper.Escape(dispatchResult.ConfirmationText),
            _telegramOptions.DefaultParseMode));

        try
        {
            await _outboxWriter.EnqueueAsync(
                new EnqueueTelegramMessageRequest(
                    dispatchResult.HogarId,
                    dispatchResult.ChatId,
                    dispatchResult.MessageType,
                    payloadJson),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram confirmation message could not be enqueued for chat {ChatId}.", dispatchResult.ChatId);
            throw;
        }
    }
}
