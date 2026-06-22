namespace Nido.Application.Telegram;

public static class TelegramCriticalEventClassifier
{
    public static bool IsCritical(TelegramCriticalEventType eventType)
    {
        return eventType == TelegramCriticalEventType.ExpirationWithinUserWindow;
    }
}
