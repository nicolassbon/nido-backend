using Nido.Application.Telegram;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramCriticalEventClassifierTests
{
    [Fact]
    public void IsCritical_ExpirationWithinUserWindow_ReturnsTrue()
    {
        Assert.True(TelegramCriticalEventClassifier.IsCritical(TelegramCriticalEventType.ExpirationWithinUserWindow));
    }

    [Fact]
    public void IsCritical_DefaultEnumValue_ReturnsFalse()
    {
        var defaultValue = default(TelegramCriticalEventType);

        Assert.False(TelegramCriticalEventClassifier.IsCritical(defaultValue));
    }

    [Fact]
    public void IsCritical_UnknownFutureEnumValue_ReturnsFalse()
    {
        var futureValue = (TelegramCriticalEventType)999;

        Assert.False(TelegramCriticalEventClassifier.IsCritical(futureValue));
    }
}
