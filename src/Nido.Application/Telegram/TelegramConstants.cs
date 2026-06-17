namespace Nido.Application.Telegram;

public static class TelegramConstants
{
    public const string ParseModeMarkdownV2 = "MarkdownV2";

    public const string ParseModeHtml = "HTML";

    public const string DeepLinkPairRoute = "/telegram/pair";

    public const int PairingCodeLength = 6;

    public const int PairingCodeMaxAttempts = 5;

    public const int PairingCodeDefaultTtlMinutes = 15;
}

public enum TelegramCriticalEventType
{
    ExpirationWithinUserWindow = 1
}

