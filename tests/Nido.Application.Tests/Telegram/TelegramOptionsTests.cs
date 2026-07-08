using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Equal(5, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(15, options.PairingTokenTtlMinutes);
        Assert.Equal(15, options.PairingCodeTtlMinutes);
        Assert.Equal(5, options.PairingRateLimitGeneratePerWindow);
        Assert.Equal(5, options.PairingCodeRateLimitValidatePerWindow);
        Assert.Equal(60, options.PairingCodeRateLimitWindowSeconds);
        Assert.True(options.DailySummaryEnabled);
        Assert.Null(options.BotToken);
        Assert.Null(options.WebhookSecretToken);
        Assert.Null(options.WebhookUrl);
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
        Assert.Equal(5, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(15, options.PairingTokenTtlMinutes);
        Assert.Equal(15, options.PairingCodeTtlMinutes);
        Assert.Equal(5, options.PairingRateLimitGeneratePerWindow);
        Assert.Equal(5, options.PairingCodeRateLimitValidatePerWindow);
        Assert.Equal(60, options.PairingCodeRateLimitWindowSeconds);
        Assert.True(options.DailySummaryEnabled);
    }

    [Fact]
    public void Bind_FromConfiguration_OverridesDefaults()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:DefaultParseMode"] = "HTML",
                ["Telegram:DailySummaryHourUtc"] = "8",
                ["Telegram:OutboxPollIntervalSeconds"] = "15",
                ["Telegram:OutboxMaxBatchSize"] = "100",
                ["Telegram:MaxAttempts"] = "7",
                ["Telegram:GroupingWindowMinutes"] = "20",
                ["Telegram:GroupingEarlySendThreshold"] = "8",
                ["Telegram:ConversationStateTtlMinutes"] = "45",
                ["Telegram:TimeoutSeconds"] = "60",
                ["Telegram:PairingTokenTtlMinutes"] = "12",
                ["Telegram:PairingCodeTtlMinutes"] = "20",
                ["Telegram:PairingRateLimitGeneratePerWindow"] = "7",
                ["Telegram:PairingCodeRateLimitValidatePerWindow"] = "8",
                ["Telegram:PairingCodeRateLimitWindowSeconds"] = "120",
                ["Telegram:DailySummaryEnabled"] = "false",
                ["Telegram:BotToken"] = "bot:secret",
                ["Telegram:WebhookSecretToken"] = "webhook-secret",
                ["Telegram:WebhookUrl"] = "https://telegram-webhook.example.test/api/webhooks/telegram"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("HTML", options.DefaultParseMode);
        Assert.Equal(8, options.DailySummaryHourUtc);
        Assert.Equal(15, options.OutboxPollIntervalSeconds);
        Assert.Equal(100, options.OutboxMaxBatchSize);
        Assert.Equal(7, options.MaxAttempts);
        Assert.Equal(20, options.GroupingWindowMinutes);
        Assert.Equal(8, options.GroupingEarlySendThreshold);
        Assert.Equal(45, options.ConversationStateTtlMinutes);
        Assert.Equal(60, options.TimeoutSeconds);
        Assert.Equal(12, options.PairingTokenTtlMinutes);
        Assert.Equal(20, options.PairingCodeTtlMinutes);
        Assert.Equal(7, options.PairingRateLimitGeneratePerWindow);
        Assert.Equal(8, options.PairingCodeRateLimitValidatePerWindow);
        Assert.Equal(120, options.PairingCodeRateLimitWindowSeconds);
        Assert.False(options.DailySummaryEnabled);
        Assert.Equal("bot:secret", options.BotToken);
        Assert.Equal("webhook-secret", options.WebhookSecretToken);
        Assert.Equal("https://telegram-webhook.example.test/api/webhooks/telegram", options.WebhookUrl);
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

    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    public void GroupingWindowMinutes_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { GroupingWindowMinutes = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.GroupingWindowMinutes)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GroupingEarlySendThreshold_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { GroupingEarlySendThreshold = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.GroupingEarlySendThreshold)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void ConversationStateTtlMinutes_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { ConversationStateTtlMinutes = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.ConversationStateTtlMinutes)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void TimeoutSeconds_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { TimeoutSeconds = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.TimeoutSeconds)));
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

        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Null(options.BotToken);
        Assert.Null(options.WebhookSecretToken);
    }

    [Fact]
    public void Options_Pipeline_BindsShippedAppSettingsDefaults()
    {
        var appsettingsPath = LocateApiAppsettingsFile();
        Assert.True(File.Exists(appsettingsPath),
            $"Expected appsettings.json at '{appsettingsPath}'.");

        var config = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("", options.BotToken);
        Assert.Equal("", options.WebhookSecretToken);
        Assert.Equal("", options.WebhookUrl);
        Assert.Equal("MarkdownV2", options.DefaultParseMode);
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Equal(1, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(15, options.PairingTokenTtlMinutes);
        Assert.Equal(15, options.PairingCodeTtlMinutes);
        Assert.Equal(5, options.PairingRateLimitGeneratePerWindow);
        Assert.Equal(5, options.PairingCodeRateLimitValidatePerWindow);
        Assert.Equal(60, options.PairingCodeRateLimitWindowSeconds);
        Assert.True(options.DailySummaryEnabled);
    }

    [Fact]
    public async Task AddTelegramModule_ValidateOnStart_FailsHostStartup_WhenOptionsInvalid()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:DailySummaryHourUtc"] = "24" // violates [Range(0, 23)]
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramModule(ctx.Configuration);
            })
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task AddTelegramModule_ValidateOnStart_AllowsHostStartup_WhenShippedDefaultsAreValid()
    {
        var appsettingsPath = LocateApiAppsettingsFile();
        Assert.True(File.Exists(appsettingsPath),
            $"Expected appsettings.json at '{appsettingsPath}'.");

        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramModule(ctx.Configuration);
            })
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    private static string LocateApiAppsettingsFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Nido.Api", "appsettings.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate src/Nido.Api/appsettings.json by walking up from "
            + AppContext.BaseDirectory);
    }

    [Theory]
    [InlineData("Telegram:GroupingWindowMinutes", 0)]
    [InlineData("Telegram:GroupingWindowMinutes", 241)]
    [InlineData("Telegram:GroupingEarlySendThreshold", 0)]
    [InlineData("Telegram:GroupingEarlySendThreshold", 101)]
    [InlineData("Telegram:ConversationStateTtlMinutes", 0)]
    [InlineData("Telegram:ConversationStateTtlMinutes", 1441)]
    [InlineData("Telegram:OutboxPollIntervalSeconds", 0)]
    [InlineData("Telegram:OutboxMaxBatchSize", 0)]
    [InlineData("Telegram:MaxAttempts", 0)]
    [InlineData("Telegram:TimeoutSeconds", 0)]
    [InlineData("Telegram:TimeoutSeconds", 301)]
    [InlineData("Telegram:PairingTokenTtlMinutes", 0)]
    [InlineData("Telegram:PairingTokenTtlMinutes", 61)]
    [InlineData("Telegram:PairingCodeTtlMinutes", 0)]
    [InlineData("Telegram:PairingCodeTtlMinutes", 61)]
    [InlineData("Telegram:PairingCodeRateLimitValidatePerWindow", 0)]
    [InlineData("Telegram:PairingCodeRateLimitValidatePerWindow", 101)]
    [InlineData("Telegram:PairingCodeRateLimitWindowSeconds", 0)]
    [InlineData("Telegram:PairingCodeRateLimitWindowSeconds", 3601)]
    public void Options_Pipeline_ThrowsValidationException_WhenRangeInvalid(string key, int value)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = value.ToString()
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<TelegramOptions>>().Value);

        var expectedProperty = key.Substring("Telegram:".Length);
        Assert.Contains(ex.Failures, f => f.Contains(expectedProperty, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Options_Pipeline_ThrowsValidationException_WhenDailySummaryHourUtcOutOfRange()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:DailySummaryHourUtc"] = "24"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<TelegramOptions>>().Value);

        Assert.Contains(ex.Failures, f => f.Contains(nameof(TelegramOptions.DailySummaryHourUtc)));
    }

    [Fact]
    public void Options_Pipeline_AppliesConfigurationOverride()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:GroupingWindowMinutes"] = "20",
                ["Telegram:GroupingEarlySendThreshold"] = "8",
                ["Telegram:ConversationStateTtlMinutes"] = "45",
                ["Telegram:DailySummaryEnabled"] = "false"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal(20, options.GroupingWindowMinutes);
        Assert.Equal(8, options.GroupingEarlySendThreshold);
        Assert.Equal(45, options.ConversationStateTtlMinutes);
        Assert.False(options.DailySummaryEnabled);
    }
}
