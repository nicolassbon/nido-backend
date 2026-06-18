using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Exceptions;

namespace Nido.Application.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<TelegramHogarAccess>();
        return services;
    }

    public static IServiceCollection AddTelegramWebhook(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<TelegramWebhookStartupValidator>();
        return services;
    }
}

internal sealed class TelegramWebhookStartupValidator : IHostedService
{
    private readonly TelegramOptions _options;

    public TelegramWebhookStartupValidator(IOptions<TelegramOptions> options)
    {
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>(capacity: 2);

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            missing.Add("Telegram:BotToken");
        }

        if (string.IsNullOrWhiteSpace(_options.WebhookSecretToken))
        {
            missing.Add("Telegram:WebhookSecretToken");
        }

        if (missing.Count > 0)
        {
            throw new TelegramConfigurationException(
                $"The following Telegram configuration value(s) are required when the webhook is registered: {string.Join(", ", missing)}.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
