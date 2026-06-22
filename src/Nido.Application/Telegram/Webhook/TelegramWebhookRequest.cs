using System;
using System.Text.Json.Serialization;

namespace Nido.Application.Telegram.Webhook;

public sealed record TelegramWebhookRequest(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramWebhookMessage? Message);

public sealed record TelegramWebhookMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("date")] long Date,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TelegramWebhookChat? Chat);

public sealed record TelegramWebhookChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string? Type);
