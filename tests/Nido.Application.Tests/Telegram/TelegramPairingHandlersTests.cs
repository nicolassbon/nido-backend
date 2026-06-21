using Nido.Application.Telegram;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Menu;
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
            new TelegramOptions { BotUsername = "nido_bot", PairingTokenTtlMinutes = 10, PairingCodeTtlMinutes = 15 });

        var result = await handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.StartsWith("https://t.me/nido_bot?start=", result.DeepLinkUrl);
        Assert.Matches(@"^\d{6}$", result.PairingCode);
        Assert.NotNull(repository.CreatedTokenHash);
        Assert.NotNull(repository.CreatedCodeHash);
        Assert.DoesNotContain(repository.CreatedTokenHash!, result.DeepLinkUrl, StringComparison.Ordinal);
        Assert.DoesNotContain(result.PairingCode, repository.CreatedCodeHash!, StringComparison.Ordinal);
        Assert.StartsWith("hash:", repository.CreatedTokenHash);
        Assert.StartsWith("hash:", repository.CreatedCodeHash);
        Assert.Equal(repository.CreatedTokenExpiresAt, result.TokenExpiresAt);
        Assert.Equal(repository.CreatedCodeExpiresAt, result.CodeExpiresAt);
    }

    [Fact]
    public async Task StartHandler_UsesDistinctTokenAndCodeTtls()
    {
        var repository = new FakePairingRepository();
        var handler = new StartTelegramPairingHandler(
            repository,
            new FakeHasher(),
            new FakeRateLimiter(),
            new TelegramOptions { BotUsername = "nido_bot", PairingTokenTtlMinutes = 7, PairingCodeTtlMinutes = 19 });

        var before = DateTime.UtcNow;
        var result = await handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.InRange(result.TokenExpiresAt, before.AddMinutes(7), after.AddMinutes(7));
        Assert.InRange(result.CodeExpiresAt, before.AddMinutes(19), after.AddMinutes(19));
        Assert.Equal(repository.CreatedTokenExpiresAt, result.TokenExpiresAt);
        Assert.Equal(repository.CreatedCodeExpiresAt, result.CodeExpiresAt);
    }

    [Fact]
    public async Task StartHandler_WhenCodeHashCollides_RetriesWithNewCode()
    {
        var repository = new FakePairingRepository { CreateArtifactsCollisionCount = 1 };
        var handler = new StartTelegramPairingHandler(
            repository,
            new FakeHasher(),
            new FakeRateLimiter(),
            new TelegramOptions { BotUsername = "nido_bot" });

        var result = await handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(2, repository.CreateArtifactsCalls);
        Assert.NotNull(repository.CreatedCodeHash);
        Assert.DoesNotContain(result.PairingCode, repository.CreatedCodeHash!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartHandler_WhenCodeHashCollidesExhaustively_ThrowsUnavailable()
    {
        var repository = new FakePairingRepository { CreateArtifactsCollisionCount = 3 };
        var handler = new StartTelegramPairingHandler(
            repository,
            new FakeHasher(),
            new FakeRateLimiter(),
            new TelegramOptions { BotUsername = "nido_bot" });

        var ex = await Assert.ThrowsAsync<TelegramPairingCodeUnavailableException>(() =>
            handler.HandleAsync(new StartTelegramPairingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(3, repository.CreateArtifactsCalls);
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
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository),
            new FakeTelegramHogarAccess(),
            new FakeConversationStateStore(),
            new FakeMenuRegistry(),
            new FakeMenuProvider());

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, "/start token-123", new TelegramWebhookChat(99, "private"))),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(99, result!.ChatId);
        Assert.Equal("hash:746f6b656e2d313233", repository.CompletedTokenHash);
    }

    [Fact]
    public async Task CompleteByCodeHandler_HashesCode_AndDelegatesToRepository()
    {
        var repository = new FakePairingRepository();
        var handler = new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter());

        await handler.HandleAsync(new CompleteTelegramPairingByCodeCommand(42, "123456"), CancellationToken.None);

        Assert.Equal("hash:313233343536", repository.CompletedCodeHash);
    }

    [Fact]
    public async Task CompleteByCodeHandler_WhenRateLimitExceeded_Throws()
    {
        var handler = new CompleteTelegramPairingByCodeHandler(
            new FakePairingRepository(),
            new FakeHasher(),
            new FakeRateLimiter { AllowCodeValidate = false });

        await Assert.ThrowsAsync<TelegramPairingRateLimitExceededException>(() =>
            handler.HandleAsync(new CompleteTelegramPairingByCodeCommand(42, "123456"), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteByCodeHandler_PassesThroughRepositoryException()
    {
        var repository = new FakePairingRepository { CodeCompletionException = new TelegramPairingCodeNotFoundException() };
        var handler = new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter());

        await Assert.ThrowsAsync<TelegramPairingCodeNotFoundException>(() =>
            handler.HandleAsync(new CompleteTelegramPairingByCodeCommand(42, "123456"), CancellationToken.None));
    }

    [Fact]
    public async Task Dispatcher_RoutesPairCommand_ToCodeHandler()
    {
        var repository = new FakePairingRepository();
        var dispatcher = new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository),
            new FakeTelegramHogarAccess(),
            new FakeConversationStateStore(),
            new FakeMenuRegistry(),
            new FakeMenuProvider());

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, "/pair 123456", new TelegramWebhookChat(77, "private"))),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(77, result!.ChatId);
        Assert.Equal("hash:313233343536", repository.CompletedCodeHash);
    }

    [Theory]
    [InlineData("/pair 12345")]
    [InlineData("/pair 1234567")]
    [InlineData("/pair abcdef")]
    [InlineData("/pair")]
    public async Task Dispatcher_IgnoresMalformedPairCommand(string text)
    {
        var repository = new FakePairingRepository();
        var dispatcher = new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository),
            new FakeTelegramHogarAccess(),
            new FakeConversationStateStore(),
            new FakeMenuRegistry(),
            new FakeMenuProvider());

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, text, new TelegramWebhookChat(77, "private"))),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(repository.CompletedCodeHash);
    }

    [Fact]
    public async Task Dispatcher_RoutesUnlinkCommand_ToUnlinkHandler()
    {
        var repository = new FakePairingRepository();
        var dispatcher = new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository),
            new FakeTelegramHogarAccess(),
            new FakeConversationStateStore(),
            new FakeMenuRegistry(),
            new FakeMenuProvider());

        var result = await dispatcher.DispatchAsync(
            new TelegramWebhookRequest(1, new TelegramWebhookMessage(1, 1, "/unlink", new TelegramWebhookChat(88, "private"))),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(88, repository.UnlinkedChatId);
    }

    [Fact]
    public async Task UnlinkPairingHandler_DelegatesUsingUserAndHogarScope()
    {
        var repository = new FakePairingRepository();
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var handler = new UnlinkTelegramPairingHandler(repository);

        var result = await handler.HandleAsync(new UnlinkTelegramPairingCommand(usuarioId, hogarId), CancellationToken.None);

        Assert.Equal(usuarioId, repository.UnlinkedUsuarioId);
        Assert.Equal(hogarId, repository.UnlinkedHogarId);
        Assert.Equal(repository.ActiveLinkResult!.ChatId, result.ChatId);
    }

    private sealed class FakePairingRepository : ITelegramPairingRepository
    {
        public string? CreatedTokenHash { get; private set; }
        public string? CreatedCodeHash { get; private set; }
        public DateTime CreatedTokenExpiresAt { get; private set; }
        public DateTime CreatedCodeExpiresAt { get; private set; }
        public string? CompletedTokenHash { get; private set; }
        public string? CompletedCodeHash { get; private set; }
        public long? UnlinkedChatId { get; private set; }
        public Guid? UnlinkedUsuarioId { get; private set; }
        public Guid? UnlinkedHogarId { get; private set; }
        public Exception? CodeCompletionException { get; init; }
        public int CreateArtifactsCollisionCount { get; init; }
        public int CreateArtifactsCalls { get; private set; }
        public TelegramChatLinkResult? ActiveLinkResult { get; set; } = new(555, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        public Task<TelegramPairingTokenResult> CreatePairingTokenAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken ct)
        {
            CreatedTokenHash = tokenHash;
            return Task.FromResult(new TelegramPairingTokenResult(Guid.NewGuid(), hogarId, usuarioId, DateTime.UtcNow, expiresAt, null, null, TelegramPairingStatus.Pending));
        }

        public Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(
            Guid hogarId,
            Guid usuarioId,
            string tokenHash,
            DateTime tokenExpiresAt,
            string codeHash,
            DateTime codeExpiresAt,
            CancellationToken ct)
        {
            CreateArtifactsCalls++;
            if (CreateArtifactsCalls <= CreateArtifactsCollisionCount)
            {
                throw new TelegramPairingCodeCollisionException();
            }

            CreatedTokenHash = tokenHash;
            CreatedCodeHash = codeHash;
            CreatedTokenExpiresAt = tokenExpiresAt;
            CreatedCodeExpiresAt = codeExpiresAt;
            var token = new TelegramPairingTokenResult(Guid.NewGuid(), hogarId, usuarioId, DateTime.UtcNow, tokenExpiresAt, null, null, TelegramPairingStatus.Pending);
            var code = new TelegramPairingCodeResult(Guid.NewGuid(), hogarId, usuarioId, 0, DateTime.UtcNow, codeExpiresAt, null, null, TelegramPairingStatus.Pending);
            return Task.FromResult((token, code));
        }

        public Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
        {
            CompletedTokenHash = tokenHash;
            return Task.FromResult(new CompleteTelegramPairingResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }

        public Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(string codeHash, long chatId, CancellationToken ct)
        {
            CompletedCodeHash = codeHash;
            if (CodeCompletionException is not null)
            {
                return Task.FromException<CompleteTelegramPairingResult>(CodeCompletionException);
            }

            return Task.FromResult(new CompleteTelegramPairingResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }

        public Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
        {
            UnlinkedChatId = chatId;
            return Task.FromResult(new UnlinkTelegramChatResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }

        public Task<UnlinkTelegramChatResult> UnlinkActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        {
            UnlinkedUsuarioId = usuarioId;
            UnlinkedHogarId = hogarId;

            var link = ActiveLinkResult is { } activeLink
                ? activeLink with { UsuarioId = usuarioId, HogarId = hogarId }
                : new TelegramChatLinkResult(555, usuarioId, hogarId, DateTime.UtcNow);

            ActiveLinkResult = link;
            return Task.FromResult(new UnlinkTelegramChatResult(link.ChatId, hogarId, usuarioId, DateTime.UtcNow));
        }

        public TelegramChatLinkResult? ActiveLink { get; set; }

        public Task<TelegramChatLinkResult?> GetActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(ActiveLink);

        public Task<TelegramChatLinkResult?> GetActiveLinkForCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(ActiveLink);
    }

    private sealed class FakeHasher : ITelegramPairingTokenHasher
    {
        public string Hash(string token) => $"hash:{Convert.ToHexStringLower(System.Text.Encoding.UTF8.GetBytes(token))}";
    }

    private sealed class FakeRateLimiter : ITelegramPairingRateLimiter
    {
        public bool AllowGenerate { get; init; } = true;
        public bool AllowConsume { get; init; } = true;
        public bool AllowCodeValidate { get; init; } = true;

        public int GenerateCalls { get; private set; }
        public int ConsumeCalls { get; private set; }
        public int CodeValidateCalls { get; private set; }

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

        public Task<bool> TryAcquireCodeValidateAsync(long chatId, CancellationToken ct)
        {
            CodeValidateCalls++;
            return Task.FromResult(AllowCodeValidate);
        }
    }

    private sealed class FakeConversationStateStore : ITelegramConversationStateStore
    {
        public Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct)
            => Task.FromResult<TelegramConversationState?>(null);

        public Task SetAsync(TelegramConversationState state, CancellationToken ct)
            => Task.CompletedTask;

        public Task ClearAsync(long chatId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeTelegramHogarAccess : ITelegramHogarAccess
    {
        public Task<TelegramChatLinkSnapshot?> GetActiveLinkAsync(long chatId, CancellationToken ct)
            => Task.FromResult<TelegramChatLinkSnapshot?>(null);

        public Task<bool> IsUserCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> IsUserAssignedToTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);
    }

    private sealed class FakeMenuRegistry : ITelegramMenuRegistry
    {
        public TelegramMenu GetDefaultMenu()
            => new(TelegramMenuCopy.MainMenuId, []);

        public TelegramMenu? Get(string menuId)
            => null;
    }

    private sealed class FakeMenuProvider : ITelegramMenuProvider
    {
        public Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct)
            => Task.FromResult(new TelegramMenuRenderResult(TelegramMenuCopy.MainMenuText));

        public Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct)
            => Task.FromResult(new TelegramMenuSelectionResult(false, string.Empty, null, false));
    }
}
