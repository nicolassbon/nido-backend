namespace Nido.Application.Telegram.Menu;

public interface ITelegramMenuReadService
{
    Task<TelegramExpiringStockReadResult> GetExpiringStockAsync(
        Guid hogarId,
        DateOnly today,
        int days,
        int limit,
        CancellationToken ct);

    Task<TelegramPantrySummary> GetPantrySummaryAsync(
        Guid hogarId,
        int categoryLimit,
        int productLimit,
        CancellationToken ct);

    Task<TelegramShoppingReadResult> GetPendingShoppingItemsAsync(
        Guid hogarId,
        int limit,
        CancellationToken ct);

    Task<TelegramPendingTasksReadResult> GetPendingAssignedTasksAsync(
        Guid hogarId,
        Guid usuarioId,
        int limit,
        CancellationToken ct);
}

public sealed record TelegramExpiringStockItem(
    string ProductName,
    decimal? Quantity,
    string? Unit,
    int ContainerCount,
    DateOnly DueDate);

public sealed record TelegramExpiringStockReadResult(
    IReadOnlyList<TelegramExpiringStockItem> Items,
    bool HasMore,
    int RemainingCount);

public sealed record TelegramPantryLineCount(string Name, int Count);

public sealed record TelegramPantrySummary(
    int TotalUnits,
    int DistinctProductCount,
    int DistinctCategoryCount,
    IReadOnlyList<TelegramPantryLineCount> TopCategories,
    IReadOnlyList<TelegramPantryLineCount> TopProducts);

public sealed record TelegramShoppingItem(
    string Name,
    decimal? Quantity,
    string? Unit,
    int ContainerCount,
    string? GroupName);

public sealed record TelegramShoppingReadResult(
    IReadOnlyList<TelegramShoppingItem> Items,
    bool HasMore,
    int RemainingCount);

public sealed record TelegramPendingTaskItem(
    string Title,
    string? Status,
    DateOnly? DueDate,
    Guid TaskId);

public sealed record TelegramPendingTasksReadResult(
    IReadOnlyList<TelegramPendingTaskItem> Items,
    bool HasMore,
    int RemainingCount);
