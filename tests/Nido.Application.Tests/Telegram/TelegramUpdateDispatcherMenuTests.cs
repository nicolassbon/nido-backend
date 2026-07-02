using Nido.Application.Tareas;
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
    public async Task DispatchAsync_StartWithoutToken_DoesNotRenderMenu()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore();
        var registry = new FakeMenuRegistry();
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, registry, provider);

        var result = await dispatcher.DispatchAsync(BuildRequest("/start"), CancellationToken.None);

        Assert.Null(result);
        Assert.Null(stateStore.LastSetState);
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

    [Fact]
    public async Task DispatchAsync_NumericReply_WithTaskCompletionPayload_CompletesTask_AndClearsPayload()
    {
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        access.IsUserAssignedToPendingTaskResult = true;
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var tareaRepository = new FakeTareaRepository
        {
            TareaCompletada = MakeTareaResult(tareaId, access.HogarId, access.UsuarioId)
        };
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), tareaRepository: tareaRepository);

        var result = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.TaskCompletionMessageType, result!.MessageType);
        Assert.Equal(TelegramMenuCopy.TaskCompletionSuccessText, result.ConfirmationText);
        Assert.Equal(1, tareaRepository.CompletarCallCount);
        Assert.Equal(tareaId, tareaRepository.LastCompletarId);
        Assert.Equal(access.UsuarioId, tareaRepository.LastCompletarPor);
        Assert.Equal(access.HogarId, tareaRepository.LastCompletarHogarId);
        // Payload must be cleared after a successful completion.
        Assert.Null(stateStore.LastSetState?.PayloadJson);
    }

    [Fact]
    public async Task DispatchAsync_NumericReply_WithOutOfRangeChoice_ReturnsRecoveryMessage_AndClearsPayload()
    {
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), provider, tareaRepository: new FakeTareaRepository());

        var result = await dispatcher.DispatchAsync(BuildRequest("9"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.TaskCompletionRecoveryMessageType, result!.MessageType);
        Assert.Contains(TelegramMenuCopy.TaskCompletionInvalidChoiceText, result.ConfirmationText, StringComparison.Ordinal);
        // The task list must be re-rendered as part of the recovery so the
        // user can choose again. The fake provider returns a deterministic
        // placeholder; in production this carries the real "Tus tareas
        // pendientes" block.
        Assert.Contains("Placeholder for option 4.", result.ConfirmationText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DispatchAsync_NumericReply_WithTaskCompletionPayload_WhenZero_ReturnsMainMenu_AndDoesNotCompleteTask()
    {
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var tareaRepository = new FakeTareaRepository();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), tareaRepository: tareaRepository);

        var result = await dispatcher.DispatchAsync(BuildRequest("0"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("interactive.menu", result!.MessageType);
        Assert.Equal(TelegramMenuCopy.MainMenuText, result.ConfirmationText);
        Assert.Equal(0, tareaRepository.CompletarCallCount);
        Assert.Null(stateStore.LastSetState?.PayloadJson);
        Assert.Equal("main-menu", stateStore.LastSetState?.MenuId);
    }

    [Fact]
    public async Task DispatchAsync_NumericReply_WhenTaskAlreadyCompleted_ReturnsAlreadyDoneMessage_AndDoesNotCallRepository()
    {
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        // Assignment + pending check fails => task is no longer completable.
        access.IsUserAssignedToPendingTaskResult = false;
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var tareaRepository = new FakeTareaRepository();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), tareaRepository: tareaRepository);

        var result = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.TaskCompletionRecoveryMessageType, result!.MessageType);
        Assert.Contains(TelegramMenuCopy.TaskCompletionAlreadyDoneText, result.ConfirmationText, StringComparison.Ordinal);
        Assert.Equal(0, tareaRepository.CompletarCallCount);
    }

    [Fact]
    public async Task DispatchAsync_TextReply_WithTaskCompletionPayload_ReturnsRecoveryMessage_WithTaskInstructions()
    {
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var provider = new FakeMenuProvider
        {
            SelectionText = $"Placeholder for option 4.\n\n{TelegramMenuCopy.TasksCompletionPrompt}"
        };
        var tareaRepository = new FakeTareaRepository();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), provider, tareaRepository: tareaRepository);

        var result = await dispatcher.DispatchAsync(BuildRequest("no"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.TaskCompletionRecoveryMessageType, result!.MessageType);
        Assert.Contains(TelegramMenuCopy.TaskCompletionInvalidChoiceText, result.ConfirmationText, StringComparison.Ordinal);
        Assert.Contains(TelegramMenuCopy.TasksCompletionPrompt, result.ConfirmationText, StringComparison.Ordinal);
        Assert.Equal(0, tareaRepository.CompletarCallCount);
    }

    [Fact]
    public async Task DispatchAsync_NumericReply_WhenChatNotLinked_ReturnsChatNotLinkedText()
    {
        var access = new FakeTelegramHogarAccess();
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow,
                new TelegramTaskCompletionPayload(
                    TelegramTaskCompletionPayload.TasksCompleteFlow,
                    new[] { new TelegramTaskCompletionChoice(1, Guid.NewGuid()) }).Serialize())
        };
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), tareaRepository: new FakeTareaRepository());

        var result = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.ChatNotLinkedText, result!.ConfirmationText);
        Assert.Equal(1, stateStore.ClearCalls);
    }

    [Fact]
    public async Task DispatchAsync_NumericReply_WhenMembershipIsStale_ReturnsAccessRevokedText()
    {
        var access = FakeTelegramHogarAccess.LinkedCurrentMember(isCurrentMember: false);
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow,
                new TelegramTaskCompletionPayload(
                    TelegramTaskCompletionPayload.TasksCompleteFlow,
                    new[] { new TelegramTaskCompletionChoice(1, Guid.NewGuid()) }).Serialize())
        };
        var repository = new FakePairingRepository();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), new FakeMenuProvider(), repository, new FakeTareaRepository());

        var result = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TelegramMenuCopy.AccessRevokedText, result!.ConfirmationText);
        Assert.Equal(99, repository.UnlinkedChatId);
    }

    [Fact]
    public async Task DispatchAsync_NumericReplyAfterCompletion_DoesNotInvokeRepository_AndFallsThroughToMainMenu()
    {
        // After a successful completion the payload is cleared; the next
        // numeric reply should hit the normal main menu path and select
        // option 1 (no task completion side effects).
        var tareaId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[] { new TelegramTaskCompletionChoice(1, tareaId) });
        var access = FakeTelegramHogarAccess.LinkedCurrentMember();
        access.IsUserAssignedToPendingTaskResult = true;
        var stateStore = new FakeConversationStateStore
        {
            CurrentState = new TelegramConversationState(99, "main-menu", DateTime.UtcNow, payload.Serialize())
        };
        var tareaRepository = new FakeTareaRepository
        {
            TareaCompletada = MakeTareaResult(tareaId, access.HogarId, access.UsuarioId)
        };
        var provider = new FakeMenuProvider();
        var dispatcher = CreateDispatcher(access, stateStore, new FakeMenuRegistry(), provider, tareaRepository: tareaRepository);

        var first = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);
        Assert.NotNull(first);
        Assert.Equal(TelegramMenuCopy.TaskCompletionMessageType, first!.MessageType);

        var second = await dispatcher.DispatchAsync(BuildRequest("1"), CancellationToken.None);

        Assert.NotNull(second);
        Assert.Equal(1, tareaRepository.CompletarCallCount);
        Assert.Equal("1", provider.LastSelection?.OptionKey);
    }

    private static TelegramUpdateDispatcher CreateDispatcher(
        ITelegramHogarAccess access,
        FakeConversationStateStore stateStore,
        ITelegramMenuRegistry registry,
        ITelegramMenuProvider provider,
        FakePairingRepository? repository = null,
        FakeTareaRepository? tareaRepository = null)
    {
        repository ??= new FakePairingRepository();
        tareaRepository ??= new FakeTareaRepository();

        return new TelegramUpdateDispatcher(
            new CompleteTelegramPairingHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new CompleteTelegramPairingByCodeHandler(repository, new FakeHasher(), new FakeRateLimiter()),
            new UnlinkTelegramChatHandler(repository, stateStore),
            access,
            stateStore,
            registry,
            provider,
            new CompletarTareaHandler(tareaRepository));
    }

    private static TareaResult MakeTareaResult(Guid id, Guid hogarId, Guid completadoPor) =>
        new(id, hogarId, "Sacar la basura", null, "completada",
            DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid(), "Creador", completadoPor, "User", null, DateTime.UtcNow);

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

        public bool IsUserAssignedToPendingTaskResult { get; set; } = false;
        public Guid HogarId => _link?.HogarId ?? Guid.Empty;
        public Guid UsuarioId => _link?.UsuarioId ?? Guid.Empty;

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

        public Task<bool> IsUserAssignedToPendingTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(IsUserAssignedToPendingTaskResult);
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
                new TelegramMenuOption("5", "Abrir Nido")
            });

        public TelegramMenu GetDefaultMenu() => _menu;

        public TelegramMenu? Get(string menuId)
            => string.Equals(menuId, _menu.Id, StringComparison.Ordinal) ? _menu : null;
    }

    private sealed class FakeMenuProvider : ITelegramMenuProvider
    {
        public (string MenuId, string OptionKey)? LastSelection { get; private set; }
        public string? SelectionText { get; set; }

        public Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct)
            => Task.FromResult(new TelegramMenuRenderResult(TelegramMenuCopy.MainMenuText));

        public Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct)
        {
            LastSelection = (menuId, optionKey);
            return Task.FromResult(new TelegramMenuSelectionResult(true, SelectionText ?? $"Placeholder for option {optionKey}.", menuId, false));
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

    private sealed class FakeTareaRepository : ITareaRepository
    {
        public TareaResult? TareaCompletada { get; set; }

        public int CompletarCallCount { get; private set; }
        public Guid LastCompletarId { get; private set; }
        public Guid LastCompletarHogarId { get; private set; }
        public Guid LastCompletarPor { get; private set; }

        public Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct) => throw new NotSupportedException();
        public Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion, DateTime? fechaLimite, string? estado, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct)
        {
            CompletarCallCount++;
            LastCompletarId = id;
            LastCompletarHogarId = hogarId;
            LastCompletarPor = completadoPor;
            return Task.FromResult(TareaCompletada);
        }
        public Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct) => Task.FromResult(new List<DistribucionDiaResult>());
    }
}
