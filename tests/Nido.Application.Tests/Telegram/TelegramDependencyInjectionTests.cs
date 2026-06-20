using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Menu;
using Nido.Application.Telegram.Pairing;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramDependencyInjectionTests
{
    [Fact]
    public void AddTelegramModule_RegistersPairingPorts()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddTelegramModule(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ITelegramHogarAccess>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingRepository>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingTokenHasher>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingRateLimiter>());
        Assert.NotNull(provider.GetRequiredService<ITelegramConversationStateStore>());
        Assert.NotNull(provider.GetRequiredService<ITelegramMenuRegistry>());
        Assert.NotNull(provider.GetRequiredService<ITelegramMenuProvider>());
        Assert.NotNull(provider.GetRequiredService<CompleteTelegramPairingByCodeHandler>());
    }

    [Fact]
    public async Task AddTelegramModule_DefaultStubsThrowHelpfulInfrastructureErrors()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddTelegramModule(configuration);

        using var provider = services.BuildServiceProvider();
        var hogarAccess = provider.GetRequiredService<ITelegramHogarAccess>();
        var repository = provider.GetRequiredService<ITelegramPairingRepository>();
        var hasher = provider.GetRequiredService<ITelegramPairingTokenHasher>();
        var rateLimiter = provider.GetRequiredService<ITelegramPairingRateLimiter>();
        var conversationStateStore = provider.GetRequiredService<ITelegramConversationStateStore>();
        var menuRegistry = provider.GetRequiredService<ITelegramMenuRegistry>();
        var menuProvider = provider.GetRequiredService<ITelegramMenuProvider>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => hogarAccess.GetActiveLinkAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => hogarAccess.IsUserCurrentMemberAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => hogarAccess.IsUserAssignedToTaskAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreatePairingTokenAsync(Guid.NewGuid(), Guid.NewGuid(), "hash", DateTime.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreatePairingArtifactsAsync(Guid.NewGuid(), Guid.NewGuid(), "token-hash", DateTime.UtcNow, "code-hash", DateTime.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CompletePairingAsync("hash", 1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CompletePairingByCodeAsync("hash", 1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UnlinkChatAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UnlinkActiveLinkAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => hasher.Hash("raw-token"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rateLimiter.TryAcquireGenerateAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rateLimiter.TryAcquireConsumeAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rateLimiter.TryAcquireCodeValidateAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => conversationStateStore.GetAsync(1, CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => menuRegistry.GetDefaultMenu());
        await Assert.ThrowsAsync<InvalidOperationException>(() => menuProvider.RenderMenuAsync(
            new TelegramMenu("main-menu", Array.Empty<TelegramMenuOption>()),
            new TelegramChatLinkSnapshot(1, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task AddTelegramWebhook_WhenSecretsMissing_StartsWithoutThrowing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddLogging();
        services.AddTelegramWebhook(configuration);

        using var provider = services.BuildServiceProvider();
        var validator = Assert.Single(provider.GetServices<IHostedService>());

        await validator.StartAsync(CancellationToken.None);
        await validator.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddTelegramWebhook_WhenSecretsPresent_ValidatorStartsAndStops()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:BotToken"] = "bot-token",
                ["Telegram:WebhookSecretToken"] = "webhook-secret"
            })
            .Build();

        services.AddLogging();
        services.AddTelegramWebhook(configuration);

        using var provider = services.BuildServiceProvider();
        var validator = Assert.Single(provider.GetServices<IHostedService>());

        await validator.StartAsync(CancellationToken.None);
        await validator.StopAsync(CancellationToken.None);
    }
}
