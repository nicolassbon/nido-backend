using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Menu;
using Nido.Application.Telegram.Pairing;
using Nido.Application.Telegram.Webhook;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramUpdateDispatcherMenuTests
{
    [Fact]
    public async Task DispatchAsync_MenuCommand_RendersMainMenu_AndStoresState()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore();
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("/menu"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(99, result!.ChatId);
        Assert.Equal("main-menu", stateStore.LastSetState?.MenuId);
        Assert.Equal(TelegramMenuCopy.MainMenuText, result.ConfirmationText);
    }

    [Fact]
    public async Task DispatchAsync_StartWithoutToken_RefreshesMainMenuState()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore();
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("/start"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("main-menu", stateStore.LastSetState?.MenuId);
        Assert.Equal(TelegramMenuCopy.MainMenuText, result!.ConfirmationText);
    }

    [Fact]
    public async Task DispatchAsync_DigitSelection_UsesStoredMenuState()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("2"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("main-menu", provider.LastSelection?.MenuId);
        Assert.Equal("2", provider.LastSelection?.OptionKey);
        Assert.Equal(TelegramMenuCopy.MainMenuText, stateStore.LastSetStateTextSnapshot);
        Assert.Equal("Placeholder for option 2.", result!.ConfirmationText);
    }

    [Fact]
    public async Task DispatchAsync_OutOfRangeDigit_ReturnsRecoveryMenu_AndKeepsState()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("9"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(TelegramMenuCopy.InvalidSelectionPrefix, result!.ConfirmationText, StringComparison.Ordinal);
        Assert.Contains(TelegramMenuCopy.MainMenuText, result.ConfirmationText, StringComparison.Ordinal);
        Assert.Equal("main-menu", stateStore.LastSetState?.MenuId);
    }

    [Fact]
    public async Task DispatchAsync_MissingStateDigit_RehydratesMainMenu()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore();
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("2"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(TelegramMenuCopy.ExpiredSelectionPrefix, result!.ConfirmationText, StringComparison.Ordinal);
        Assert.Contains(TelegramMenuCopy.MainMenuText, result.ConfirmationText, StringComparison.Ordinal);
        Assert.Equal("main-menu", stateStore.LastSetState?.MenuId);
    }

    [Fact]
    public async Task DispatchAsync_SlashCommand_TakesPrecedenceOverDigits()
    {
        var repository = new FakePairingRepository();
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), repository);

        var result = await dispatcher.DispatchAsync(BuildRequest("/unlink"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(99, repository.UnlinkedChatId);
        Assert.Equal(1, stateStore.ClearCalls);
    }

    [Fact]
    public async Task DispatchAsync_StartWithToken_ClearsExistingConversationState_AfterSuccessfulPairing()
    {
        var repository = new FakePairingRepository();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var dispatcher = CreateDispatcher(FakeTelegramHogarAccess.LinkedCurrentMember(), stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), repository);

        var result = await dispatcher.DispatchAsync(BuildRequest("/start pairing-token"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, stateStore.ClearCalls);
        Assert.Null(stateStore.CurrentState);
    }

    [Fact]
    public async Task DispatchAsync_PairWithCode_ClearsExistingConversationState_AfterSuccessfulPairing()
    {
        var repository = new FakePairingRepository();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var dispatcher = CreateDispatcher(FakeTelegramHogarAccess.LinkedCurrentMember(), stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), repository);

        var result = await dispatcher.DispatchAsync(BuildRequest("/pair 123456"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, stateStore.ClearCalls);
        Assert.Null(stateStore.CurrentState);
    }

    [Fact]
    public async Task DispatchAsync_MenuCommand_WhenChatNotLinked_ReturnsRecoveryMessage_AndClearsState()
    {
        var access = new FakeTelegramHogarAccess();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, null)
        };
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider());

        var result = await dispatcher.DispatchAsync(BuildRequest("/menu"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.ChatNotLinkedText, result!.ConfirmationText);
        Assert.Null(stateStore.LastSetState);
        Assert.Equal(1, stateStore.ClearCalls);
    }

    [Fact]
    public async Task DispatchAsync_MenuCommand_WhenMembershipIsStale_UnlinksAndReturnsRecoveryMessage()
    {
        var repository = new FakePairingRepository();
        var access = FakeTelegramHogarAccess.LinkedCurrentMember(isCurrentMember: false);
        var stateStore = new FakeConversationStateStore();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), repository);

        var result = await dispatcher.DispatchAsync(BuildRequest("/menu"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.AccessRevokedText, result!.ConfirmationText);
        Assert.Equal(99, repository.UnlinkedChatId);
        Assert.Equal(1, stateStore.ClearCalls);
    }

    private static TelegramUpdateDispatcher CreateDispatcher(
        ITelegramHogarAccess access,
        FakeConversationStateStore stateStore,
        ITelegramMenuRegistry registry,
        ITelegramMenuProvider provider,
        FakePairingRepository? repository = null)
    {
        repository ??= new FakePairingRepository();

        return new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository, stateStore),
            access,
            stateStore,
            registry,
            provider);
    }

    private static TelegramWebhookRequest BuildRequest(string text)
        => new(1, new TelegramWebhookMessage(1, 1, text, new TelegramWebhookChat(99, "private")));

    private sealed class FakeConversationStateStore : ITelegramConversationStateStore
    {
        public TelegramConversationState? CurrentState { get; set; }
        public TelegramConversationState? LastSetState { get; private set; }
        public int ClearCalls { get; private set; }
        public string? LastSetStateTextSnapshot { get; set; }

        public Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct)
            => Task.FromResult(CurrentState);

        public Task SetAsync(TelegramConversationState state, CancellationToken ct)
        {
            LastSetState = state;
            CurrentState = state;
            LastSetStateTextSnapshot = TelegramMenuCopy.MainMenuText;
            return Task.CompletedTask;
        }

        public Task ClearAsync(long chatId, CancellationToken ct)
        {
            ClearCalls++;
            CurrentState = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTelegramHogarAccess : ITelegramHogarAccess
    {
        private readonly TelegramChatLinkSnapshot? _link;
        private readonly bool _isCurrentMember;

        private FakeTelegramHogarAccess(TelegramChatLinkSnapshot? link, bool isCurrentMember)
        {
            _link = link;
            _isCurrentMember = isCurrentMember;
        }

        public FakeTelegramHogarAccess()
            : this(null, false)
        {
        }

        public static FakeTelegramHogarAccess LinkedCurrentMember(bool isCurrentMember = true)
            => new(new TelegramChatLinkSnapshot(99, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null), isCurrentMember);

        public Task<TelegramChatLinkSnapshot?> GetActiveLinkAsync(long chatId, CancellationToken ct)
            => Task.FromResult(_link);

        public Task<bool> IsUserCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(_isCurrentMember);

        public Task<bool> IsUserAssignedToTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);
    }

    private sealed class FakeMenuRegistry : ITelegramMenuRegistry
    {
        private readonly TelegramMenu _menu = new(
            "main-menu",
            new[]
            {
                new TelegramMenuOption("1", "Ver productos por vencer"),
                new TelegramMenuOption("2", "Ver resumen de alacena"),
                new TelegramMenuOption("3", "Ver lista de compras"),
                new TelegramMenuOption("4", "Ver tareas pendientes"),
                new TelegramMenuOption("5", "Abrir Nido"),
                new TelegramMenuOption("6", "Configurar notificaciones")
            });

        public TelegramMenu GetDefaultMenu() => _menu;

        public TelegramMenu? Get(string menuId)
            => string.Equals(menuId, _menu.Id, StringComparison.Ordinal) ? _menu : null;
    }

    private sealed class FakeMenuProvider : ITelegramMenuProvider
    {
        public (string MenuId, string OptionKey)? LastSelection { get; private set; }

        public Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct)
            => Task.FromResult(new TelegramMenuRenderResult(TelegramMenuCopy.MainMenuText));

        public Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct)
        {
            LastSelection = (menuId, optionKey);
            return Task.FromResult(new TelegramMenuSelectionResult(true, $"Placeholder for option {optionKey}.", menuId, false));
        }
    }

    private sealed class FakePairingRepository : ITelegramPairingRepository
    {
        public long? UnlinkedChatId { get; private set; }

        public Task<TelegramPairingTokenResult> CreatePairingTokenAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime tokenExpiresAt, string codeHash, DateTime codeExpiresAt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
            => Task.FromResult(new CompleteTelegramPairingResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        public Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(string codeHash, long chatId, CancellationToken ct)
            => Task.FromResult(new CompleteTelegramPairingResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        public Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
        {
            UnlinkedChatId = chatId;
            return Task.FromResult(new UnlinkTelegramChatResult(chatId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        }

        public Task<UnlinkTelegramChatResult> UnlinkActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TelegramChatLinkResult?> GetActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult<TelegramChatLinkResult?>(null);

        public Task<TelegramChatLinkResult?> GetActiveLinkForCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult<TelegramChatLinkResult?>(null);
    }

    private sealed class FakeHasher : ITelegramPairingTokenHasher
    {
        public string Hash(string token) => token;
    }

    private sealed class FakeRateLimiter : ITelegramPairingRateLimiter
    {
        public Task<bool> TryAcquireGenerateAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> TryAcquireConsumeAsync(long chatId, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> TryAcquireCodeValidateAsync(long chatId, CancellationToken ct) => Task.FromResult(true);
    }
}
