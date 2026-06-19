using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Exceptions;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramWebhookOptionsTests
{
    [Fact]
    public void Defaults_ApplyWebhookTuningFromDocumentedValues()
    {
        var options = new TelegramOptions();

        Assert.Equal(102_400, options.WebhookMaxPayloadBytes);
        Assert.Equal(100, options.WebhookRateLimitPermitPerWindow);
        Assert.Equal(60, options.WebhookRateLimitWindowSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_485_761)]
    public void WebhookMaxPayloadBytes_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { WebhookMaxPayloadBytes = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.WebhookMaxPayloadBytes)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100_001)]
    public void WebhookRateLimitPermitPerWindow_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { WebhookRateLimitPermitPerWindow = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.WebhookRateLimitPermitPerWindow)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3_601)]
    public void WebhookRateLimitWindowSeconds_OutOfRange_FailsValidation(int value)
    {
        var options = new TelegramOptions { WebhookRateLimitWindowSeconds = value };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(TelegramOptions.WebhookRateLimitWindowSeconds)));
    }

    [Fact]
    public void Bind_FromConfiguration_OverridesWebhookDefaults()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:WebhookSecretToken"] = "secret-token",
                ["Telegram:WebhookMaxPayloadBytes"] = "65536",
                ["Telegram:WebhookRateLimitPermitPerWindow"] = "10",
                ["Telegram:WebhookRateLimitWindowSeconds"] = "30"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddTelegramModule(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        Assert.Equal("secret-token", options.WebhookSecretToken);
        Assert.Equal(65_536, options.WebhookMaxPayloadBytes);
        Assert.Equal(10, options.WebhookRateLimitPermitPerWindow);
        Assert.Equal(30, options.WebhookRateLimitWindowSeconds);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_FailsWithTelegramConfigurationException_WhenSecretTokenEmpty()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "bot:secret",
                    ["Telegram:WebhookSecretToken"] = ""
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Equal("TELEGRAM_CONFIGURATION", exception.Code);
        Assert.Contains("WebhookSecretToken", exception.Message);
        Assert.DoesNotContain("BotToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_FailsWithTelegramConfigurationException_WhenSecretTokenMissing()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "bot:secret"
                    // WebhookSecretToken is intentionally absent.
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Contains("WebhookSecretToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_FailsWithTelegramConfigurationException_WhenBotTokenEmpty()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "",
                    ["Telegram:WebhookSecretToken"] = "shhh"
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Equal("TELEGRAM_CONFIGURATION", exception.Code);
        Assert.Contains("BotToken", exception.Message);
        Assert.DoesNotContain("WebhookSecretToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_FailsWithTelegramConfigurationException_WhenBotTokenWhitespace()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "   ",
                    ["Telegram:WebhookSecretToken"] = "shhh"
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Contains("BotToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_FailsWithTelegramConfigurationException_WhenBotTokenMissing()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:WebhookSecretToken"] = "shhh"
                    // BotToken is intentionally absent.
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Contains("BotToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_ListsBothMissingKeys_WhenBothCredentialsAbsent()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                // No Telegram section at all.
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<TelegramConfigurationException>(() => host.StartAsync());
        Assert.Contains("BotToken", exception.Message);
        Assert.Contains("WebhookSecretToken", exception.Message);
    }

    [Fact]
    public async Task AddTelegramWebhook_Startup_AllowsHost_WhenBothCredentialsConfigured()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "bot:secret",
                    ["Telegram:WebhookSecretToken"] = "shhh"
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10485761")]
    public async Task AddTelegramWebhook_Startup_FailsWithOptionsValidationException_WhenWebhookMaxPayloadBytesOutOfRange(string configured)
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "bot:secret",
                    ["Telegram:WebhookSecretToken"] = "shhh",
                    ["Telegram:WebhookMaxPayloadBytes"] = configured
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Contains("WebhookMaxPayloadBytes", exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("100001")]
    public async Task AddTelegramWebhook_Startup_FailsWithOptionsValidationException_WhenWebhookRateLimitPermitPerWindowOutOfRange(string configured)
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "bot:secret",
                    ["Telegram:WebhookSecretToken"] = "shhh",
                    ["Telegram:WebhookRateLimitPermitPerWindow"] = configured
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddTelegramWebhook(ctx.Configuration);
            })
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Contains("WebhookRateLimitPermitPerWindow", exception.Message);
    }
}
