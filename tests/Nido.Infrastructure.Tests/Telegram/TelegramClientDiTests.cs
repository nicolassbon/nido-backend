using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Infrastructure.Telegram;
using Nido.Infrastructure.Telegram.Messaging;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramClientDiTests
{
    [Fact]
    public void AddNidoInfrastructure_RegistersITelegramClient_ResolvesAsTelegramClient()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token",
            ["Telegram:TimeoutSeconds"] = "45"
        });

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ITelegramClient>();

        Assert.IsType<TelegramClient>(client);
    }

    [Fact]
    public void AddNidoInfrastructure_MissingBotToken_ResolvesDisabledClient()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration();

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITelegramClient>();

        Assert.IsType<DisabledTelegramClient>(client);
    }

    [Fact]
    public void AddNidoInfrastructure_AppliesTimeoutSeconds()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token",
            ["Telegram:TimeoutSeconds"] = "45"
        });

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var client = (TelegramClient)provider.GetRequiredService<ITelegramClient>();

        var http = (HttpClient)typeof(TelegramClient)
            .GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(client)!;

        Assert.Equal(TimeSpan.FromSeconds(45), http.Timeout);
    }

    [Fact]
    public void AddNidoInfrastructure_AppliesBaseAddress()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token"
        });

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var client = (TelegramClient)provider.GetRequiredService<ITelegramClient>();

        var http = (HttpClient)typeof(TelegramClient)
            .GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(client)!;

        Assert.Equal(new Uri("https://api.telegram.org/botmy-token/"), http.BaseAddress);
    }

    [Fact]
    public void AddTelegramSenderWorker_WithoutBotToken_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration();

        services.AddTelegramSenderWorker(config);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramSenderWorker));
    }

    [Fact]
    public void AddTelegramSenderWorker_WithoutBotToken_DoesNotRegisterBatchingWorker()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration();

        services.AddTelegramSenderWorker(config);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramBatchingWorker));
    }

    [Fact]
    public void AddTelegramSenderWorker_WithBotToken_RegistersSenderWorker()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token"
        });

        services.AddTelegramSenderWorker(config);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramSenderWorker));
    }

    [Fact]
    public void AddTelegramSenderWorker_WithBotToken_RegistersBatchingWorker()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token"
        });

        services.AddTelegramSenderWorker(config);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramBatchingWorker));
    }

    [Fact]
    public void AddNidoInfrastructure_DoesNotRegisterTelegramSenderWorker()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token"
        });

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramSenderWorker));
    }

    [Fact]
    public void AddNidoInfrastructure_DoesNotRegisterTelegramBatchingWorker()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfiguration(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "my-token"
        });

        services.AddOptions<TelegramOptions>()
            .Bind(config.GetSection(TelegramOptions.SectionName));

        services.AddNidoInfrastructure(config);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TelegramBatchingWorker));
    }

    private static IConfiguration CreateMinimalConfiguration(Dictionary<string, string?>? extra = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            ["Google:ClientId"] = "test-client-id"
        };

        if (extra is not null)
        {
            foreach (var kv in extra)
                values[kv.Key] = kv.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
