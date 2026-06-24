using System.Text.Json.Serialization;

namespace Nido.Infrastructure.Telegram;

internal sealed class TelegramApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public TelegramApiResult? Result { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("parameters")]
    public TelegramApiErrorParameters? Parameters { get; set; }
}

internal sealed class TelegramApiResult
{
    [JsonPropertyName("message_id")]
    public long? MessageId { get; set; }
}

internal sealed class TelegramApiErrorParameters
{
    [JsonPropertyName("retry_after")]
    public int? RetryAfter { get; set; }
}
