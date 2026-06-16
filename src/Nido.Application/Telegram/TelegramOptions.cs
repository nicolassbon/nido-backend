using System.ComponentModel.DataAnnotations;

namespace Nido.Application.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string? BotToken { get; init; }

    public string? WebhookSecretToken { get; init; }

    [Required]
    public string DefaultParseMode { get; init; } = "MarkdownV2";

    [Required]
    public string FrontEndBaseUrl { get; init; } = string.Empty;

    [Range(0, 23, ErrorMessage = "DailySummaryHourUtc must be between 0 and 23.")]
    public int DailySummaryHourUtc { get; init; } = 9;

    [Range(1, 86_400, ErrorMessage = "OutboxPollIntervalSeconds must be between 1 and 86400.")]
    public int OutboxPollIntervalSeconds { get; init; } = 30;

    [Range(1, 1_000, ErrorMessage = "OutboxMaxBatchSize must be between 1 and 1000.")]
    public int OutboxMaxBatchSize { get; init; } = 50;

    [Range(1, 100, ErrorMessage = "MaxAttempts must be between 1 and 100.")]
    public int MaxAttempts { get; init; } = 5;
}
