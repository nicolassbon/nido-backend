using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram.Client;

namespace Nido.Infrastructure.Telegram;

public sealed class TelegramClient : ITelegramClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TelegramClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelegramClient(HttpClient http, ILogger<TelegramClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TelegramSendResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode = null,
        TelegramInlineKeyboardMarkup? replyMarkup = null,
        CancellationToken ct = default)
    {
        if (chatId == 0)
        {
            return new TelegramSendResult.Error(
                new TelegramValidationError("chatId must not be 0."));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramSendResult.Error(
                new TelegramValidationError("text is required."));
        }

        var payload = new
        {
            chat_id = chatId,
            text = text,
            parse_mode = parseMode,
            reply_markup = replyMarkup is null
                ? null
                : new
                {
                    inline_keyboard = replyMarkup.InlineKeyboard
                        .Select(row => row.Select(b => new { text = b.Text, callback_data = b.CallbackData }))
                }
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("sendMessage", payload, JsonOptions, ct);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(JsonOptions, ct);

                if (apiResponse?.Ok == true && apiResponse.Result?.MessageId is long messageId)
                {
                    _logger.LogInformation(
                        "Telegram message sent successfully. ChatId={ChatId} MessageId={MessageId}",
                        chatId, messageId);

                    return new TelegramSendResult.Success(new TelegramMessageSent(messageId));
                }

                _logger.LogWarning(
                    "Telegram returned success status but unexpected payload. ChatId={ChatId}",
                    chatId);

                return new TelegramSendResult.Error(
                    new TelegramTransientError("Unexpected response payload from Telegram."));
            }

            var errorBody = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(JsonOptions, ct);
            var description = errorBody?.Description ?? $"HTTP {(int)response.StatusCode}";
            var errorCode = errorBody?.ErrorCode ?? (int)response.StatusCode;

            _logger.LogWarning(
                "Telegram API error. ChatId={ChatId} ErrorCode={ErrorCode} Description={Description}",
                chatId, errorCode, description);

            return errorCode switch
            {
                429 => new TelegramSendResult.Error(
                    new TelegramRateLimitError(description, errorBody?.Parameters?.RetryAfter ?? 0)),
                400 or 401 or 403 => new TelegramSendResult.Error(
                    new TelegramPermanentError(description)),
                _ => new TelegramSendResult.Error(
                    new TelegramTransientError(description))
            };
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            _logger.LogWarning(ex, "Telegram request was cancelled. ChatId={ChatId}", chatId);
            return new TelegramSendResult.Error(
                new TelegramTransientError("Request was cancelled."));
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != ct)
        {
            _logger.LogWarning(ex, "Telegram request timed out. ChatId={ChatId}", chatId);
            return new TelegramSendResult.Error(
                new TelegramTransientError("Request timed out."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Telegram request failed due to network error. ChatId={ChatId}", chatId);
            return new TelegramSendResult.Error(
                new TelegramTransientError($"Network error: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Telegram response. ChatId={ChatId}", chatId);
            return new TelegramSendResult.Error(
                new TelegramTransientError($"Failed to parse response: {ex.Message}"));
        }
    }
}
