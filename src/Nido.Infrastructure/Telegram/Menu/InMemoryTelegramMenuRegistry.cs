using Nido.Application.Telegram.Menu;

namespace Nido.Infrastructure.Telegram.Menu;

public sealed class InMemoryTelegramMenuRegistry : ITelegramMenuRegistry
{
    private static readonly TelegramMenu MainMenu = new(
        TelegramMenuCopy.MainMenuId,
        new[]
        {
            new TelegramMenuOption("1", "Ver productos por vencer"),
            new TelegramMenuOption("2", "Ver resumen de alacena"),
            new TelegramMenuOption("3", "Ver lista de compras"),
            new TelegramMenuOption("4", "Ver tareas pendientes"),
            new TelegramMenuOption("5", "Abrir Nido")
        });

    public TelegramMenu GetDefaultMenu() => MainMenu;

    public TelegramMenu? Get(string menuId)
        => string.Equals(menuId, MainMenu.Id, StringComparison.Ordinal) ? MainMenu : null;
}
