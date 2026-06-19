using Nido.Application.Telegram;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Pairing;
using Nido.Application.Telegram.Webhook;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramPairingHandlersTests
{
    [Fact]
    public async Task StartHandler_ReturnsDeepLink_AndStoresOnlyHash()
    {
        var repository = new FakePairingRepository();
        var hasher = new FakeHasher();
        var rateLimiter = new FakeRateLimiter();
        var handler = new StartTelegramPairingHandler(
            repository,
            hasher,
            rateLimiter,
            new TelegramOptions { BotUsername = "nido_bot", PairingTokenTtlMinutes = 15 });

        var result = await handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.StartsWith("https://t.me/nido_bot?start=", result.DeepLinkUrl);
        Assert.NotNull(repository.CreatedTokenHash);
        Assert.DoesNotContain(repository.CreatedTokenHash!, result.DeepLinkUrl, StringComparison.Ordinal);
        Assert.StartsWith("hash:", repository.CreatedTokenHash);
    }

    [Fact]
    public async Task StartHandler_WhenRateLimitExceeded_Throws()
    {
        var handler = new StartTelegramPairingHandler(
            new FakePairingRepository(),
            new FakeHasher(),
            new FakeRateLimiter { AllowGenerate = false },
            new TelegramOptions { BotUsername = "nido_bot" });

        await Assert.ThrowsAsync<TelegramPairingRateLimitExceededException>(() =>
            handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task StartHandler_WhenBotUsernameEmpty_AlwaysReturnsConfigurationError_BeforeRateLimiter()
    {
        var rateLimiter = new FakeRateLimiter { AllowGenerate = false };
        var handler = new StartTelegramPairingHandler(
            new FakePairingRepository(),
            new FakeHasher(),
            rateLimiter,
            new TelegramOptions { BotUsername = "" });

        await Assert.ThrowsAsync<TelegramConfigurationException>(() =>
            handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        await Assert.ThrowsAsync<TelegramConfigurationException>(() =>
            handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(0, rateLimiter.GenerateCalls);
    }

    [Fact]
    public async Task CompleteHandler_WhenRateLimitExceeded_Throws()
    {
        var handler = new CompleteTelegramPairingHandler(
            new FakePairingRepository(),
            new FakeHasher(),
            new FakeRateLimiter { AllowConsume = false });

        await Assert.ThrowsAsync<TelegramPairingRateLimitExceededException>(() =>
            handler.HandleAsync(new CompleteTelegramPairingCommand(10, "token"), CancellationToken.None));
    }

    [Fact]
    public async Task Dispatcher_RoutesStartCommand_ToPairingHandler()
    {
        var repository = new FakePairingRepository();
        var dispatcher = new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository));

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, "/start token-123", new TelegramWebhookChat(99, "private"))),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(99, result!.ChatId);
        Assert.Equal("hash:token-123", repository.CompletedTokenHash);
    }

    [Fact]
    public async Task Dispatcher_RoutesUnlinkCommand_ToUnlinkHandler()
    {
        var repository = new FakePairingRepository();
        var dispatcher = new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository));

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, "/unlink", new TelegramWebhookChat(88, "private"))),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(88, repository.UnlinkedChatId);
    }

    private sealed class FakePairingRepository : ITelegramPairingRepository
    {
        public string? CreatedTokenHash { get; private set; }
        public string? CompletedTokenHash { get; private set; }
        public long? UnlinkedChatId { get; private set; }

        public Task<TelegramPairingTokenResult> CreatePairingTokenAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken ct)
        {
            CreatedTokenHash = tokenHash;
            return Task.FromResult(new TelegramPairingTokenResult(Guid.NewGuid(), hogarId, usuarioId, DateTime.UtcNow, expiresAt, null, null, TelegramPairingStatus.Pending));
        }

        public Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
        {
            CompletedTokenHash = tokenHash;
            return Task.FromResult(new CompleteTelegramPairingResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }

        public Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
        {
            UnlinkedChatId = chatId;
            return Task.FromResult(new UnlinkTelegramChatResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }
    }

    private sealed class FakeHasher : ITelegramPairingTokenHasher
    {
        public string Hash(string token) => $"hash:{token}";
    }

    private sealed class FakeRateLimiter : ITelegramPairingRateLimiter
    {
        public bool AllowGenerate { get; init; } = true;
        public bool AllowConsume { get; init; } = true;

        public int GenerateCalls { get; private set; }
        public int ConsumeCalls { get; private set; }

        public Task<bool> TryAcquireGenerateAsync(Guid usuarioId, CancellationToken ct)
        {
            GenerateCalls++;
            return Task.FromResult(AllowGenerate);
        }

        public Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct)
        {
            ConsumeCalls++;
            return Task.FromResult(AllowConsume);
        }
    }
}
