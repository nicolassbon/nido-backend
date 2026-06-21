using System.ComponentModel.DataAnnotations;
using Nido.Application.Telegram;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramPairingOptionsTests
{
    [Fact]
    public void Defaults_ApplyPairingValuesFromSpec()
    {
        var options = new TelegramOptions();

        Assert.Equal(string.Empty, options.BotUsername);
        Assert.Equal(15, options.PairingTokenTtlMinutes);
        Assert.Equal(5, options.PairingRateLimitGeneratePerWindow);
        Assert.Equal(5, options.PairingRateLimitConsumePerWindow);
        Assert.Equal(60, options.PairingRateLimitWindowSeconds);
    }

    [Theory]
    [InlineData(nameof(TelegramOptions.PairingTokenTtlMinutes), 0)]
    [InlineData(nameof(TelegramOptions.PairingTokenTtlMinutes), 61)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitGeneratePerWindow), 0)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitGeneratePerWindow), 101)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitConsumePerWindow), 0)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitConsumePerWindow), 101)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitWindowSeconds), 0)]
    [InlineData(nameof(TelegramOptions.PairingRateLimitWindowSeconds), 3601)]
    public void PairingRanges_OutOfRange_FailValidation(string propertyName, int value)
    {
        var options = new TelegramOptions();
        typeof(TelegramOptions).GetProperty(propertyName)!.SetValue(options, value);

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(propertyName));
    }
}
