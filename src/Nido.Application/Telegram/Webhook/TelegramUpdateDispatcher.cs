using System.Text.RegularExpressions;
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
    ITelegramMenuProvider menuProvider)
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
            new MissingTelegramMenuProvider())
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
            await completePairingHandler.HandleAsync(new CompleteTelegramPairingCommand(chatId.Value, parts[1]), ct);
            await conversationStateStore.ClearAsync(chatId.Value, ct);
            return new TelegramDispatchResult(chatId.Value, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido.");
        }

        if (string.Equals(command, "/pair", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            var code = parts[1];
            if (!PairingCodeRegex.IsMatch(code))
            {
                return null;
            }

            await completePairingByCodeHandler.HandleAsync(new CompleteTelegramPairingByCodeCommand(chatId.Value, code), ct);
            await conversationStateStore.ClearAsync(chatId.Value, ct);
            return new TelegramDispatchResult(chatId.Value, "¡Listo! Este chat ya quedó vinculado a tu hogar en Nido.");
        }

        if (string.Equals(command, "/unlink", StringComparison.OrdinalIgnoreCase))
        {
            await unlinkTelegramChatHandler.HandleAsync(new UnlinkTelegramChatCommand(chatId.Value), ct);
            return new TelegramDispatchResult(chatId.Value, "Listo. Este chat quedó desvinculado de tu hogar en Nido.");
        }

        if (IsMenuCommand(command))
        {
            return await HandleMenuCommandAsync(chatId.Value, ct);
        }

        if (MenuSelectionRegex.IsMatch(text))
        {
            return await HandleMenuSelectionAsync(chatId.Value, text, ct);
        }

        return null;
    }

    private static bool IsMenuCommand(string command)
        => string.Equals(command, "/menu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "/inicio", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "/help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase);

    private async Task<TelegramDispatchResult> HandleMenuCommandAsync(long chatId, CancellationToken ct)
    {
        try
        {
            var link = await EnsureMenuAccessAsync(chatId, ct);
            var menu = menuRegistry.GetDefaultMenu();
            var render = await menuProvider.RenderMenuAsync(menu, link, ct);
            await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, null), ct);
            return new TelegramDispatchResult(chatId, render.Text);
        }
        catch (TelegramChatNotLinkedException)
        {
            await ClearStateBestEffortAsync(chatId, ct);
            return new TelegramDispatchResult(chatId, TelegramMenuCopy.ChatNotLinkedText);
        }
        catch (TelegramHogarAccessDeniedException)
        {
            return new TelegramDispatchResult(chatId, TelegramMenuCopy.AccessRevokedText);
        }
    }

    private async Task<TelegramDispatchResult?> HandleMenuSelectionAsync(long chatId, string text, CancellationToken ct)
    {
        try
        {
            var link = await EnsureMenuAccessAsync(chatId, ct);
            var state = await conversationStateStore.GetAsync(chatId, ct);

            if (state is null)
            {
                return await RenderRecoveryToMainMenuAsync(chatId, link, TelegramMenuCopy.ExpiredSelectionPrefix, ct);
            }

            var menu = menuRegistry.Get(state.MenuId) ?? menuRegistry.GetDefaultMenu();
            if (!menu.ContainsOption(text))
            {
                var render = await menuProvider.RenderMenuAsync(menu, link, ct);
                await conversationStateStore.SetAsync(new TelegramConversationState(chatId, menu.Id, DateTime.UtcNow, state.PayloadJson), ct);
                return new TelegramDispatchResult(chatId, TelegramMenuCopy.BuildRecoveryText(TelegramMenuCopy.InvalidSelectionPrefix, render.Text));
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
                await conversationStateStore.SetAsync(new TelegramConversationState(chatId, selection.NextMenuId ?? menu.Id, DateTime.UtcNow, state.PayloadJson), ct);
            }

            return new TelegramDispatchResult(chatId, selection.Text);
        }
        catch (TelegramChatNotLinkedException)
        {
            await ClearStateBestEffortAsync(chatId, ct);
            return new TelegramDispatchResult(chatId, TelegramMenuCopy.ChatNotLinkedText);
        }
        catch (TelegramHogarAccessDeniedException)
        {
            return new TelegramDispatchResult(chatId, TelegramMenuCopy.AccessRevokedText);
        }
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
        return new TelegramDispatchResult(chatId, TelegramMenuCopy.BuildRecoveryText(prefix, render.Text));
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

public sealed record TelegramDispatchResult(long ChatId, string ConfirmationText);
