namespace Nido.Application.Telegram.Menu;

public static class TelegramMenuCopy
{
    public const string MainMenuId = "main-menu";
    public const string InvalidSelectionPrefix = "No reconozco esa opción.";
    public const string ExpiredSelectionPrefix = "Tu menú anterior ya no está disponible.";
    public const string ChatNotLinkedText = "Este chat no está vinculado a un hogar de Nido. Abrí la app y generá un nuevo enlace de vinculación para continuar.";
    public const string AccessRevokedText = "Tu acceso a ese hogar ya no está disponible. Desvinculé este chat por seguridad. Si corresponde, volvé a vincularlo desde la app.";

    public const string MainMenuText =
        "Elegí una opción respondiendo con un número:\n"
        + "1. Ver productos por vencer\n"
        + "2. Ver resumen de alacena\n"
        + "3. Ver lista de compras\n"
        + "4. Ver tareas pendientes\n"
        + "5. Abrir Nido\n"
        + "6. Configurar notificaciones";

    public static string BuildRecoveryText(string prefix, string menuText)
        => $"{prefix}\n\n{menuText}";
}
