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
        Assert.Equal(string.Empty, options.FrontEndBaseUrl);
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Equal(15, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
        Assert.True(options.DailySummaryEnabled);
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
        Assert.Equal(15, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
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
                ["Telegram:FrontEndBaseUrl"] = "https://nido.example.com",
                ["Telegram:DailySummaryHourUtc"] = "8",
                ["Telegram:OutboxPollIntervalSeconds"] = "15",
                ["Telegram:OutboxMaxBatchSize"] = "100",
                ["Telegram:MaxAttempts"] = "7",
                ["Telegram:GroupingWindowMinutes"] = "20",
                ["Telegram:GroupingEarlySendThreshold"] = "8",
                ["Telegram:ConversationStateTtlMinutes"] = "45",
                ["Telegram:DailySummaryEnabled"] = "false",
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
        Assert.Equal(20, options.GroupingWindowMinutes);
        Assert.Equal(8, options.GroupingEarlySendThreshold);
        Assert.Equal(45, options.ConversationStateTtlMinutes);
        Assert.False(options.DailySummaryEnabled);
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

        // Resolving IOptions<T>.Value runs the registered ValidateDataAnnotations()
        // pipeline. With empty config the defaults must satisfy every [Range] and
        // the optional secret placeholders (BotToken / WebhookSecretToken /
        // FrontEndBaseUrl) are intentionally absent in the shared base slice.
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Null(options.BotToken);
        Assert.Null(options.WebhookSecretToken);
        Assert.Equal(string.Empty, options.FrontEndBaseUrl);
    }

    [Fact]
    public void Options_Pipeline_BindsShippedAppSettingsDefaults()
    {
        // Loads the actual src/Nido.Api/appsettings.json file (not a hand-typed
        // mirror) and binds its "Telegram" section through the real options
        // pipeline. This protects the shipped config contract from drift:
        // if any default in appsettings.json changes, this test must be updated
        // consciously rather than drifting silently alongside the JSON.
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

        // Resolving IOptions<T>.Value triggers the full ValidateDataAnnotations()
        // pipeline. If any [Range] constraint were violated by the shipped
        // defaults, this call would throw OptionsValidationException.
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        // Hardcoded expectations are the documented contract: changing them
        // without updating appsettings.json (or vice versa) is a deliberate,
        // review-visible action.
        Assert.Equal("", options.BotToken);
        Assert.Equal("", options.WebhookSecretToken);
        Assert.Equal("MarkdownV2", options.DefaultParseMode);
        Assert.Equal("", options.FrontEndBaseUrl);
        Assert.Equal(9, options.DailySummaryHourUtc);
        Assert.Equal(30, options.OutboxPollIntervalSeconds);
        Assert.Equal(50, options.OutboxMaxBatchSize);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Equal(15, options.GroupingWindowMinutes);
        Assert.Equal(5, options.GroupingEarlySendThreshold);
        Assert.Equal(30, options.ConversationStateTtlMinutes);
        Assert.True(options.DailySummaryEnabled);
    }

    [Fact]
    public async Task AddTelegramModule_ValidateOnStart_FailsHostStartup_WhenOptionsInvalid()
    {
        // Fail-fast contract: with an out-of-range value bound through the
        // configuration section, host.StartAsync() must throw because the
        // ValidateOnStart() registration runs the validation pipeline before
        // the host reports started. If ValidateOnStart() were removed from
        // DependencyInjection.cs, validation would become lazy and this
        // test would start passing (host boots fine, error surfaces only on
        // first IOptions<T>.Value access) — proving the test actually guards
        // the fail-fast registration.
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
        // Counterpart of the failing-host test: with the shipped appsettings
        // defaults, the host must boot cleanly through the ValidateOnStart()
        // path. Together with the failing case, this proves the registration
        // is engaged and the shipped defaults are accepted as the contract.
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
        // Walk up from the test bin directory to the repo root, then return
        // the path to src/Nido.Api/appsettings.json. This keeps the test
        // independent of the current working directory or solution layout
        // assumptions.
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

        // The real options pipeline is what would fail at ValidateOnStart in the
        // host. Asserting it here proves the binding + data-annotation wiring is
        // actually engaged, not just that the attribute exists on the class.
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
        // Proves the binding path actually picks up overrides, not just defaults.
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
