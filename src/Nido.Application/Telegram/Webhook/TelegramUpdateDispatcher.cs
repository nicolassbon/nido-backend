using System.Text.RegularExpressions;
using Nido.Application.Gamificacion;
using Nido.Application.Tareas;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Menu;
using Nido.Application.Telegram.Pairing;

namespace Nido.Application.Telegram.Webhook;

public sealed partial class TelegramUpdateDispatcher(
    CompleteTelegramPairingHandler completePairingHandler,
    CompleteTelegramPairingByCodeHandler completePairingByCodeHandler,
    UnlinkTelegramChatHandler unlinkTelegramChatHandler,
    ITelegramHogarAccess hogarAccess,
    ITelegramConversationStateStore conversationStateStore,
    ITelegramMenuRegistry menuRegistry,
    ITelegramMenuProvider menuProvider,
    CompletarTareaHandler completarTareaHandler)
{
    private static readonly Regex PairingCodeRegex = GetPairingCodeRegex();
    private static readonly Regex MenuSelectionRegex = GetMenuSelectionRegex();

    public TelegramUpdateDispatcher(
        CompleteTelegramPairingHandler completePairingHandler,
        CompleteTelegramPairingByCodeHandler completePairingByCodeHandler,
        UnlinkTelegramChatHandler unlinkTelegramChatHandler)
        : this(
            completePairingHandler,
            completePairingByCodeHandler,
            unlinkTelegramChatHandler,
            new MissingTelegramHogarAccess(),
            new MissingTelegramConversationStateStore(),
            new MissingTelegramMenuRegistry(),
            new MissingTelegramMenuProvider(),
            new CompletarTareaHandler(new MissingTareaRepository(), new MissingGamificationUnlockMaterializer()))
    {
    }

    public async Task<TelegramDispatchResult?> DispatchAsync(TelegramWebhookRequest request, CancellationToken ct)
    {
        var text = request.Message?.Text?.Trim();
        var chatId = request.Message?.Chat?.Id;

        if (string.IsNullOrWhiteSpace(text) || chatId is null or 0)
        {
            return null;
        }

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var command = parts[0].Split('@', 2)[0];

        if (string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            var pairing = await completePairingHandler.HandleAsync(new CompleteTelegramPairingCommand(chatId.Value, parts[1]), ct);
            await conversationStateStore.ClearAsync(chatId.Value, ct);
            return new TelegramDispatchResult(chatId.Value, pairing.HogarId, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido. Usá /menu para ver las opciones disponibles.", "interactive.pairing.complete");
        }

        if (string.Equals(command, "/pair", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            var code = parts[1];
            if (!PairingCodeRegex.IsMatch(code))
            {
                return null;
            }

            var pairing = await completePairingByCodeHandler.HandleAsync(new CompleteTelegramPairingByCodeCommand(chatId.Value, code), ct);
            await conversationStateStore.ClearAsync(chatId.Value, ct);
            return new TelegramDispatchResult(chatId.Value, pairing.HogarId, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido. Usá /menu para ver las opciones disponibles.", "interactive.pairing.complete");
        }

        if (string.Equals(command, "/unlink", StringComparison.OrdinalIgnoreCase))
        {
            var unlink = await unlinkTelegramChatHandler.HandleAsync(new UnlinkTelegramChatCommand(chatId.Value), ct);
            return new TelegramDispatchResult(chatId.Value, unlink.HogarId, "Listo. Este chat quedó desvinculado de tu hogar en Nido.", "interactive.unlink");
        }

        if (IsMenuCommand(command))
        {
            return await HandleMenuCommandAsync(chatId.Value, ct);
        }

        if (MenuSelectionRegex.IsMatch(text))
        {
            return await HandleMenuSelectionAsync(chatId.Value, text, ct);
        }

        return await HandleTaskCompletionTextIfActiveAsync(chatId.Value, text, ct);
    }

    private static bool IsMenuCommand(string command)
        => string.Equals(command, "/menu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "/inicio", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "/help", StringComparison.OrdinalIgnoreCase);

    private async Task<TelegramDispatchResult> HandleMenuCommandAsync(long chatId, CancellationToken ct)
    {
        try
        {
            var link = await EnsureMenuAccessAsync(chatId, ct);
            var menu = menuRegistry.GetDefaultMenu();
            var render = await menuProvider.RenderMenuAsync(menu, link, ct);
            await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, null), ct);
            return new TelegramDispatchResult(chatId, link.HogarId, render.Text, "interactive.menu");
        }
        catch (TelegramChatNotLinkedException)
        {
            await ClearStateBestEffortAsync(chatId, ct);
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.ChatNotLinkedText, "interactive.menu.recovery");
        }
        catch (TelegramHogarAccessDeniedException)
        {
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.AccessRevokedText, "interactive.menu.recovery");
        }
    }

    private async Task<TelegramDispatchResult?> HandleMenuSelectionAsync(long chatId, string text, CancellationToken ct)
    {
        try
        {
            var link = await EnsureMenuAccessAsync(chatId, ct);
            var state = await conversationStateStore.GetAsync(chatId, ct);

            var taskPayload = state is null
                ? null
                : TelegramTaskCompletionPayload.TryParse(state.PayloadJson);
            if (taskPayload is not null)
            {
                return await HandleTaskCompletionSelectionAsync(chatId, link, taskPayload, text, ct);
            }

            if (state is null)
            {
                return await RenderRecoveryToMainMenuAsync(chatId, link, TelegramMenuCopy.ExpiredSelectionPrefix, ct);
            }

            var menu = menuRegistry.Get(state.MenuId) ?? menuRegistry.GetDefaultMenu();
            if (!menu.ContainsOption(text))
            {
                var render = await menuProvider.RenderMenuAsync(menu, link, ct);
                await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, state.PayloadJson), ct);
                return new TelegramDispatchResult(chatId, link.HogarId, TelegramMenuCopy.BuildRecoveryText(TelegramMenuCopy.InvalidSelectionPrefix, render.Text), "interactive.menu.recovery");
            }

            var selection = await menuProvider.SelectAsync(menu.Id, text, link, ct);
            if (!selection.Handled)
            {
                return null;
            }

            if (selection.ShouldClearState)
            {
                await conversationStateStore.ClearAsync(chatId, ct);
            }
            else
            {
                var newPayload = selection.PayloadJson ?? state.PayloadJson;
                await conversationStateStore.SetAsync(new TelegramConversationState(chatId, selection.NextMenuId ?? menu.Id, DateTime.UtcNow, newPayload), ct);
            }

            return new TelegramDispatchResult(chatId, link.HogarId, selection.Text, $"interactive.{menu.Id}.{text}");
        }
        catch (TelegramChatNotLinkedException)
        {
            await ClearStateBestEffortAsync(chatId, ct);
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.ChatNotLinkedText, "interactive.menu.recovery");
        }
        catch (TelegramHogarAccessDeniedException)
        {
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.AccessRevokedText, "interactive.menu.recovery");
        }
    }

    private async Task<TelegramDispatchResult> HandleTaskCompletionSelectionAsync(
        long chatId,
        TelegramChatLinkSnapshot link,
        TelegramTaskCompletionPayload payload,
        string text,
        CancellationToken ct)
    {
        if (text == "0")
        {
            return await RenderMainMenuAsync(chatId, link, ct);
        }

        if (!int.TryParse(text, out var index)
            || index < 1
            || !payload.TryFindChoice(index, out var choice)
            || choice is null)
        {
            return await RenderTaskCompletionRecoveryAsync(chatId, link, TelegramMenuCopy.TaskCompletionInvalidChoiceText, ct);
        }

        var isAuthorized = await hogarAccess.IsUserAssignedToPendingTaskAsync(
            link.UsuarioId,
            choice.TaskId,
            link.HogarId,
            ct);

        if (!isAuthorized)
        {
            return await RenderTaskCompletionRecoveryAsync(chatId, link, TelegramMenuCopy.TaskCompletionAlreadyDoneText, ct);
        }

        var result = await completarTareaHandler.Handle(
            new CompletarTareaCommand(choice.TaskId, link.HogarId, link.UsuarioId),
            ct);

        if (result is null)
        {
            return await RenderTaskCompletionRecoveryAsync(chatId, link, TelegramMenuCopy.TaskCompletionAlreadyDoneText, ct);
        }

        await conversationStateStore.SetAsync(
            new TelegramConversationState(chatId, TelegramMenuCopy.MainMenuId, DateTime.UtcNow, null),
            ct);

        return new TelegramDispatchResult(
            chatId,
            link.HogarId,
            TelegramMenuCopy.TaskCompletionSuccessText,
            TelegramMenuCopy.TaskCompletionMessageType);
    }

    private async Task<TelegramDispatchResult?> HandleTaskCompletionTextIfActiveAsync(long chatId, string text, CancellationToken ct)
    {
        try
        {
            var state = await conversationStateStore.GetAsync(chatId, ct);
            var taskPayload = state is null
                ? null
                : TelegramTaskCompletionPayload.TryParse(state.PayloadJson);

            if (taskPayload is null)
            {
                return null;
            }

            var link = await EnsureMenuAccessAsync(chatId, ct);
            return await RenderTaskCompletionRecoveryAsync(chatId, link, TelegramMenuCopy.TaskCompletionInvalidChoiceText, ct);
        }
        catch (TelegramChatNotLinkedException)
        {
            await ClearStateBestEffortAsync(chatId, ct);
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.ChatNotLinkedText, "interactive.menu.recovery");
        }
        catch (TelegramHogarAccessDeniedException)
        {
            return new TelegramDispatchResult(chatId, Guid.Empty, TelegramMenuCopy.AccessRevokedText, "interactive.menu.recovery");
        }
    }

    private async Task<TelegramDispatchResult> RenderMainMenuAsync(long chatId, TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        var menu = menuRegistry.GetDefaultMenu();
        var render = await menuProvider.RenderMenuAsync(menu, link, ct);
        await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, null), ct);
        return new TelegramDispatchResult(chatId, link.HogarId, render.Text, "interactive.menu");
    }

    private async Task<TelegramDispatchResult> RenderTaskCompletionRecoveryAsync(
        long chatId,
        TelegramChatLinkSnapshot link,
        string prefix,
        CancellationToken ct)
    {
        var refreshed = await menuProvider.SelectAsync(TelegramMenuCopy.MainMenuId, "4", link, ct);
        var recoveryText = refreshed.Handled
            ? TelegramMenuCopy.BuildRecoveryText(prefix, refreshed.Text)
            : TelegramMenuCopy.BuildRecoveryText(prefix, TelegramMenuCopy.TaskCompletionEmptyListText);

        var newPayload = refreshed.PayloadJson;
        await conversationStateStore.SetAsync(
            new TelegramConversationState(chatId, TelegramMenuCopy.MainMenuId, DateTime.UtcNow, newPayload),
            ct);

        return new TelegramDispatchResult(
            chatId,
            link.HogarId,
            recoveryText,
            TelegramMenuCopy.TaskCompletionRecoveryMessageType);
    }

    private async Task<TelegramChatLinkSnapshot> EnsureMenuAccessAsync(long chatId, CancellationToken ct)
    {
        var link = await hogarAccess.GetActiveLinkAsync(chatId, ct)
            ?? throw new TelegramChatNotLinkedException();

        if (await hogarAccess.IsUserCurrentMemberAsync(link.UsuarioId, link.HogarId, ct))
        {
            return link;
        }

        await unlinkTelegramChatHandler.HandleAsync(new UnlinkTelegramChatCommand(chatId), ct);
        throw new TelegramHogarAccessDeniedException();
    }

    private async Task<TelegramDispatchResult> RenderRecoveryToMainMenuAsync(long chatId, TelegramChatLinkSnapshot link, string prefix, CancellationToken ct)
    {
        var menu = menuRegistry.GetDefaultMenu();
        var render = await menuProvider.RenderMenuAsync(menu, link, ct);
        await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, null), ct);
        return new TelegramDispatchResult(chatId, link.HogarId, TelegramMenuCopy.BuildRecoveryText(prefix, render.Text), "interactive.menu.recovery");
    }

    private async Task ClearStateBestEffortAsync(long chatId, CancellationToken ct)
    {
        try
        {
            await conversationStateStore.ClearAsync(chatId, ct);
        }
        catch
        {
            // Best effort cleanup only; webhook acknowledgement must still succeed.
        }
    }

    [GeneratedRegex(@"^\d{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex GetPairingCodeRegex();

    [GeneratedRegex(@"^\d{1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex GetMenuSelectionRegex();
}

public sealed record TelegramDispatchResult(long ChatId, Guid HogarId, string ConfirmationText, string MessageType)
{
    public TelegramDispatchResult(long chatId, string confirmationText)
        : this(chatId, Guid.Empty, confirmationText, "interactive.message")
    {
    }
}

internal sealed class MissingGamificationUnlockMaterializer : IGamificationUnlockMaterializer
{
    public Task<IReadOnlyList<int>> MaterializeEligibleUnlocksAsync(Guid usuarioId, CancellationToken ct)
        => throw new InvalidOperationException("IGamificationUnlockMaterializer requires gamification module registration.");
}

internal sealed class MissingTareaRepository : Nido.Application.Tareas.ITareaRepository
{
    public Task<List<Nido.Application.Tareas.TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<List<Nido.Application.Tareas.TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<Nido.Application.Tareas.TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<Nido.Application.Tareas.TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<Nido.Application.Tareas.TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion, DateTime? fechaLimite, string? estado, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<Nido.Application.Tareas.TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<Nido.Application.Tareas.TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");

    public Task<List<Nido.Application.Tareas.DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct)
        => throw new InvalidOperationException("ITareaRepository requires infrastructure registration.");
}
