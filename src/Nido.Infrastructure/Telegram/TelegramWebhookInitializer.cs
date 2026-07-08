using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;

namespace Nido.Infrastructure.Telegram;

internal sealed class TelegramWebhookInitializer : ITelegramWebhookInitializer
{
    private readonly HttpClient _http;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramWebhookInitializer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelegramWebhookInitializer(
        HttpClient http,
        IOptions<TelegramOptions> options,
        ILogger<TelegramWebhookInitializer> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var payload = new
        {
            url = _options.WebhookUrl,
            secret_token = _options.WebhookSecretToken,
            allowed_updates = new[] { "message" }
        };

        using var response = await _http.PostAsJsonAsync("setWebhook", payload, JsonOptions, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<TelegramSetWebhookResponse>(JsonOptions, cancellationToken);

        if (response.IsSuccessStatusCode && body?.Ok == true)
        {
            _logger.LogInformation(
                "Telegram webhook registered successfully. WebhookUrl={WebhookUrl}",
                _options.WebhookUrl);
            return;
        }

        var description = body?.Description ?? $"HTTP {(int)response.StatusCode}";
        var errorCode = body?.ErrorCode ?? (int)response.StatusCode;

        throw new InvalidOperationException(
            $"Telegram setWebhook failed. ErrorCode={errorCode} Description={description}");
    }
}

internal sealed class TelegramSetWebhookResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }
}
