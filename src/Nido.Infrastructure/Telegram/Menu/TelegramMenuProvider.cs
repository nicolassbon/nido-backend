using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Menu;

namespace Nido.Infrastructure.Telegram.Menu;

public sealed class TelegramMenuProvider : ITelegramMenuProvider
{
    public Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct)
        => Task.FromResult(new TelegramMenuRenderResult(TelegramMenuCopy.MainMenuText));

    public Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct)
        => Task.FromResult(new TelegramMenuSelectionResult(
            true,
            $"Placeholder for option {optionKey}.",
            menuId,
            false));
}
