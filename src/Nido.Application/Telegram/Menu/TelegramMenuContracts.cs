using Nido.Application.Telegram.Authorization;

namespace Nido.Application.Telegram.Menu;

public sealed record TelegramMenu(string Id, IReadOnlyList<TelegramMenuOption> Options)
{
    public bool ContainsOption(string optionKey)
        => Options.Any(option => string.Equals(option.Key, optionKey, StringComparison.Ordinal));
}

public sealed record TelegramMenuOption(string Key, string Label);

public sealed record TelegramMenuRenderResult(string Text);

public sealed record TelegramMenuSelectionResult(
    bool Handled,
    string Text,
    string? NextMenuId,
    bool ShouldClearState,
    string? PayloadJson = null);

public interface ITelegramMenuRegistry
{
    TelegramMenu GetDefaultMenu();
    TelegramMenu? Get(string menuId);
}

public interface ITelegramMenuProvider
{
    Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct);
    Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct);
}
