using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram.Pairing;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Webhook;

namespace Nido.Application.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped(sp => sp.GetRequiredService<IOptions<TelegramOptions>>().Value);
        services.AddScoped<StartTelegramPairingHandler>();
        services.AddScoped<CompleteTelegramPairingHandler>();
        services.AddScoped<CompleteTelegramPairingByCodeHandler>();
        services.AddScoped<UnlinkTelegramChatHandler>();
        services.AddScoped<TelegramUpdateDispatcher>();

        services.TryAddScoped<ITelegramHogarAccess, MissingTelegramHogarAccess>();
        services.TryAddScoped<ITelegramPairingRepository, MissingTelegramPairingRepository>();
        services.TryAddScoped<ITelegramPairingTokenHasher, MissingTelegramPairingTokenHasher>();
        services.TryAddScoped<ITelegramPairingRateLimiter, MissingTelegramPairingRateLimiter>();
        return services;
    }

    public static IServiceCollection AddTelegramWebhook(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTelegramModule(configuration);

        services.AddHostedService<TelegramWebhookStartupValidator>();
        return services;
    }
}

internal sealed class MissingTelegramHogarAccess : ITelegramHogarAccess
{
    public Task<TelegramChatLinkSnapshot?> GetActiveLinkAsync(long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramHogarAccess requires infrastructure registration.");

    public Task<bool> IsUserCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramHogarAccess requires infrastructure registration.");

    public Task<bool> IsUserAssignedToTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramHogarAccess requires infrastructure registration.");
}

internal sealed class MissingTelegramPairingRepository : ITelegramPairingRepository
{
    public Task<TelegramPairingTokenResult> CreatePairingTokenAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRepository requires infrastructure registration.");

    public Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime tokenExpiresAt,
        string codeHash,
        DateTime codeExpiresAt,
        CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRepository requires infrastructure registration.");

    public Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRepository requires infrastructure registration.");

    public Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(string codeHash, long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRepository requires infrastructure registration.");

    public Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRepository requires infrastructure registration.");
}

internal sealed class MissingTelegramPairingTokenHasher : ITelegramPairingTokenHasher
{
    public string Hash(string token)
        => throw new InvalidOperationException("ITelegramPairingTokenHasher requires infrastructure registration.");
}

internal sealed class MissingTelegramPairingRateLimiter : ITelegramPairingRateLimiter
{
    public Task<bool> TryAcquireGenerateAsync(Guid usuarioId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRateLimiter requires infrastructure registration.");

    public Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRateLimiter requires infrastructure registration.");

    public Task<bool> TryAcquireCodeValidateAsync(long chatId, CancellationToken ct)
        => throw new InvalidOperationException("ITelegramPairingRateLimiter requires infrastructure registration.");
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
