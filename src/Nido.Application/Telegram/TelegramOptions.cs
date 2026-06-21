using System.ComponentModel.DataAnnotations;

namespace Nido.Application.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string? BotToken { get; init; }

    public string? WebhookSecretToken { get; init; }

    public bool HasBotToken => !string.IsNullOrWhiteSpace(BotToken);

    public bool HasWebhookSecretToken => !string.IsNullOrWhiteSpace(WebhookSecretToken);

    public bool IsWebhookConfigured => HasBotToken && HasWebhookSecretToken;

    public string BotUsername { get; set; } = string.Empty;

    [Required]
    public string DefaultParseMode { get; init; } = "MarkdownV2";

    [Range(0, 23, ErrorMessage = "DailySummaryHourUtc must be between 0 and 23.")]
    public int DailySummaryHourUtc { get; init; } = 9;

    [Range(1, 86_400, ErrorMessage = "OutboxPollIntervalSeconds must be between 1 and 86400.")]
    public int OutboxPollIntervalSeconds { get; init; } = 30;

    [Range(1, 86_400, ErrorMessage = "InteractiveOutboxPollIntervalSeconds must be between 1 and 86400.")]
    public int InteractiveOutboxPollIntervalSeconds { get; init; } = 2;

    [Range(1, 1_000, ErrorMessage = "OutboxMaxBatchSize must be between 1 and 1000.")]
    public int OutboxMaxBatchSize { get; init; } = 50;

    [Range(1, 100, ErrorMessage = "OutboxMaxInteractiveAttempts must be between 1 and 100.")]
    public int OutboxMaxInteractiveAttempts { get; init; } = 3;

    [Range(1, 100, ErrorMessage = "MaxAttempts must be between 1 and 100.")]
    public int MaxAttempts { get; init; } = 5;

    [Range(1, 240, ErrorMessage = "GroupingWindowMinutes must be between 1 and 240.")]
    public int GroupingWindowMinutes { get; init; } = 5;

    [Range(1, 100, ErrorMessage = "GroupingEarlySendThreshold must be between 1 and 100.")]
    public int GroupingEarlySendThreshold { get; set; } = 5;

    [Range(1, 1440, ErrorMessage = "ConversationStateTtlMinutes must be between 1 and 1440.")]
    public int ConversationStateTtlMinutes { get; init; } = 30;

    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300.")]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(1, 60, ErrorMessage = "PairingTokenTtlMinutes must be between 1 and 60.")]
    public int PairingTokenTtlMinutes { get; set; } = 15;

    [Range(1, 100, ErrorMessage = "PairingRateLimitGeneratePerWindow must be between 1 and 100.")]
    public int PairingRateLimitGeneratePerWindow { get; set; } = 5;

    [Range(1, 100, ErrorMessage = "PairingRateLimitConsumePerWindow must be between 1 and 100.")]
    public int PairingRateLimitConsumePerWindow { get; set; } = 5;

    [Range(1, 3_600, ErrorMessage = "PairingRateLimitWindowSeconds must be between 1 and 3600.")]
    public int PairingRateLimitWindowSeconds { get; set; } = 60;

    [Range(1, 60, ErrorMessage = "PairingCodeTtlMinutes must be between 1 and 60.")]
    public int PairingCodeTtlMinutes { get; set; } = 15;

    [Range(1, 100, ErrorMessage = "PairingCodeRateLimitValidatePerWindow must be between 1 and 100.")]
    public int PairingCodeRateLimitValidatePerWindow { get; set; } = 5;

    [Range(1, 3_600, ErrorMessage = "PairingCodeRateLimitWindowSeconds must be between 1 and 3600.")]
    public int PairingCodeRateLimitWindowSeconds { get; set; } = 60;

    public bool DailySummaryEnabled { get; init; } = true;

    [Range(1, 10_485_760, ErrorMessage = "WebhookMaxPayloadBytes must be between 1 and 10485760.")]
    public int WebhookMaxPayloadBytes { get; init; } = 102_400;

    [Range(1, 100_000, ErrorMessage = "WebhookRateLimitPermitPerWindow must be between 1 and 100000.")]
    public int WebhookRateLimitPermitPerWindow { get; init; } = 100;

    [Range(1, 3_600, ErrorMessage = "WebhookRateLimitWindowSeconds must be between 1 and 3600.")]
    public int WebhookRateLimitWindowSeconds { get; init; } = 60;
}
