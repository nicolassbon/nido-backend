namespace Nido.Application.Telegram.Menu;

public static class TelegramMenuCopy
{
    public const string MainMenuId = "main-menu";
    public const string InvalidSelectionPrefix = "No reconozco esa opción.";
    public const string ExpiredSelectionPrefix = "Tu menú anterior ya no está disponible.";
    public const string ChatNotLinkedText = "Este chat no está vinculado a un hogar de Nido. Abrí la app y generá un nuevo enlace de vinculación para continuar.";
    public const string AccessRevokedText = "Tu acceso a ese hogar ya no está disponible. Desvinculé este chat por seguridad. Si corresponde, volvé a vincularlo desde la app.";

    public const string MainMenuText =
        "🏠 Nido — ¿Qué querés hacer?\n\n"
        + "1. Ver productos por vencer\n"
        + "2. Ver resumen de alacena\n"
        + "3. Ver lista de compras\n"
        + "4. Ver tareas pendientes\n"
        + "5. Abrir Nido\n\n"
        + "Respondé con el número de la opción.";

    public static string BuildRecoveryText(string prefix, string menuText)
        => $"{prefix}\n\n{menuText}";

    public const string TaskCompletionHeaderText = "✅ Tareas pendientes";
    public const string TaskCompletionBackOptionText = "0. Volver al Menú";
    public const string TasksCompletionPrompt = "Respondé con el número de la tarea completada.";

    public const string TaskCompletionSuccessText =
        "Listo, marqué la tarea como completada. Volvé al menú cuando quieras seguir.";

    public const string TaskCompletionInvalidChoiceText =
        "Ese número no corresponde a una tarea de la lista actual.";

    public const string TaskCompletionAlreadyDoneText =
        "Esa tarea ya estaba completada o ya no la tenés asignada.";

    public const string TaskCompletionEmptyListText = "No tenés tareas pendientes asignadas.";

    public const string TaskCompletionMessageType = "interactive.tasks.complete";
    public const string TaskCompletionRecoveryMessageType = "interactive.tasks.complete.recovery";
}
