using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramOptionsTests
{
    [Fact]
    public void SectionName_IsTelegram()
    {
        Assert.Equal("Telegram", TelegramOptions.SectionName);
    }

    [Fact]
    public void Defaults_WhenNothingBound_UseDocumentedValues()
    {
        var options = new TelegramOptions();

        Assert.Equal("MarkdownV2", options.DefaultParseMode);
        Assert.Equal(string.Empty, options.FrontEndBaseUrl);
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Null(options.BotToken);
        Assert.Null(options.WebhookSecretToken);
    }

    [Fact]
    public void Bind_FromEmptyConfigurationSection_RegistersOptionsWithDefaults()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("MarkdownV2", options.DefaultParseMode);
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
    }

    [Fact]
    public void Bind_FromConfiguration_OverridesDefaults()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:DefaultParseMode"] = "HTML",
                ["Telegram:FrontEndBaseUrl"] = "https://nido.example.com",
                ["Telegram:DailySummaryHourUtc"] = "8",
                ["Telegram:OutboxPollIntervalSeconds"] = "15",
                ["Telegram:OutboxMaxBatchSize"] = "100",
                ["Telegram:MaxAttempts"] = "7",
                ["Telegram:BotToken"] = "bot:secret",
                ["Telegram:WebhookSecretToken"] = "webhook-secret"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("HTML", options.DefaultParseMode);
        Assert.Equal("https://nido.example.com", options.FrontEndBaseUrl);
        Assert.Equal(8, options.DailySummaryHourUtc);
        Assert.Equal(15, options.OutboxPollIntervalSeconds);
        Assert.Equal(100, options.OutboxMaxBatchSize);
        Assert.Equal(7, options.MaxAttempts);
        Assert.Equal("bot:secret", options.BotToken);
        Assert.Equal("webhook-secret", options.WebhookSecretToken);
    }

    [Fact]
    public void DailySummaryHourUtc_OutOfRange_FailsValidation()
    {
        var options = new TelegramOptions { DailySummaryHourUtc = 24 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.DailySummaryHourUtc)));
    }

    [Fact]
    public void OutboxPollIntervalSeconds_BelowMinimum_FailsValidation()
    {
        var options = new TelegramOptions { OutboxPollIntervalSeconds = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.OutboxPollIntervalSeconds)));
    }

    [Fact]
    public void OutboxMaxBatchSize_BelowMinimum_FailsValidation()
    {
        var options = new TelegramOptions { OutboxMaxBatchSize = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.OutboxMaxBatchSize)));
    }

    [Fact]
    public void MaxAttempts_BelowMinimum_FailsValidation()
    {
        var options = new TelegramOptions { MaxAttempts = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.MaxAttempts)));
    }

    [Fact]
    public void AddTelegramModule_DoesNotThrow_WhenBotTokenIsMissing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();

        // Build should succeed and options should resolve even without secrets
        // (webhook secret validation is the consumer PR's responsibility)
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;
        Assert.Null(options.BotToken);
    }
}
