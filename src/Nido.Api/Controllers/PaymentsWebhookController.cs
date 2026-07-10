using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nido.Application.Payments;
using Nido.Api.Middleware;

namespace Nido.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/mercadopago")]
public sealed class PaymentsWebhookController : ControllerBase
{
    private readonly ProcessWebhookHandler _handler;
    private readonly ILogger<PaymentsWebhookController> _logger;

    public PaymentsWebhookController(
        ProcessWebhookHandler handler,
        ILogger<PaymentsWebhookController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MercadoPagoWebhookPayloadSizeMiddleware.MaxPayloadBytes)]
    public async Task<IActionResult> Post(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["x-signature"].ToString();
        var requestId = Request.Headers["x-request-id"].ToString();
        var dataId = ResolveSignatureManifestDataId(Request.Query);

        var result = await _handler.Handle(new ProcessWebhookCommand(payload, signature, requestId, dataId), ct);

        if (result.Outcome == ProcessWebhookOutcome.Unauthorized)
        {
            LogUnauthorizedWebhookDiagnostics(signature, dataId);
        }

        return result.Outcome switch
        {
            ProcessWebhookOutcome.Unauthorized => Unauthorized(),
            ProcessWebhookOutcome.RetryableFailure => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => Ok()
        };
    }

    public static string ResolveSignatureManifestDataId(IQueryCollection query)
    {
        var dataId = query["data.id"].ToString();
        return string.IsNullOrWhiteSpace(dataId)
            ? query["id"].ToString()
            : dataId;
    }

    private void LogUnauthorizedWebhookDiagnostics(string signatureHeader, string selectedDataId)
    {
        var signatureParts = ParseSignatureHeader(signatureHeader);
        signatureParts.TryGetValue("ts", out var timestamp);

        _logger.LogWarning(
            "Unauthorized Mercado Pago webhook. Outcome: {Outcome}, HasSignature: {HasSignature}, HasTimestamp: {HasTimestamp}, HasRequestId: {HasRequestId}, SelectedDataId: {SelectedDataId}, Topic: {Topic}, Type: {Type}",
            ProcessWebhookOutcome.Unauthorized,
            !string.IsNullOrWhiteSpace(signatureHeader),
            !string.IsNullOrWhiteSpace(timestamp),
            Request.Headers.ContainsKey("x-request-id"),
            selectedDataId,
            Request.Query["topic"].ToString(),
            Request.Query["type"].ToString());
    }

    private static IReadOnlyDictionary<string, string> ParseSignatureHeader(string signatureHeader)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in signatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(p => p.Length == 2))
        {
            if (!parts.TryAdd(segment[0], segment[1]))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        return parts;
    }

}
