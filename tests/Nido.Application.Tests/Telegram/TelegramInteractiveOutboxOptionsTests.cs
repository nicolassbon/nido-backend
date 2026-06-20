using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramInteractiveOutboxOptionsTests
{
    [Fact]
    public void Defaults_UseInteractiveOutboxValues()
    {
        var options = new TelegramOptions();

        Assert.Equal(2, options.InteractiveOutboxPollIntervalSeconds);
        Assert.Equal(3, options.OutboxMaxInteractiveAttempts);
    }

    [Fact]
    public void Bind_FromConfiguration_OverridesInteractiveOutboxValues()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:InteractiveOutboxPollIntervalSeconds"] = "5",
                ["Telegram:OutboxMaxInteractiveAttempts"] = "6"
            })
            .Build();

        services.AddTelegramModule(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal(5, options.InteractiveOutboxPollIntervalSeconds);
        Assert.Equal(6, options.OutboxMaxInteractiveAttempts);
    }

    [Theory]
    [InlineData(0, nameof(TelegramOptions.InteractiveOutboxPollIntervalSeconds))]
    [InlineData(86_401, nameof(TelegramOptions.InteractiveOutboxPollIntervalSeconds))]
    [InlineData(0, nameof(TelegramOptions.OutboxMaxInteractiveAttempts))]
    [InlineData(101, nameof(TelegramOptions.OutboxMaxInteractiveAttempts))]
    public void Validation_RejectsOutOfRangeInteractiveOutboxValues(int value, string propertyName)
    {
        var options = propertyName switch
        {
            nameof(TelegramOptions.InteractiveOutboxPollIntervalSeconds) => new TelegramOptions { InteractiveOutboxPollIntervalSeconds = value },
            nameof(TelegramOptions.OutboxMaxInteractiveAttempts) => new TelegramOptions { OutboxMaxInteractiveAttempts = value },
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null)
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(propertyName));
    }
}
