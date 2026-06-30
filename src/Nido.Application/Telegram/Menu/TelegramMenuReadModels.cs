namespace Nido.Application.Telegram.Menu;

public interface ITelegramMenuReadService
{
    /// <summary>
    /// Returns household stock items whose expiration date is within the
    /// given window (inclusive of <paramref name="today"/>), ordered by due
    /// date. The result is bounded to <paramref name="limit"/> items and
    /// exposes an overflow signal when more rows are available.
    /// </summary>
    Task<TelegramExpiringStockReadResult> GetExpiringStockAsync(
        Guid hogarId,
        DateOnly today,
        int days,
        int limit,
        CancellationToken ct);

    /// <summary>
    /// Returns a household pantry summary: total container units, distinct
    /// product/category counts, and the top categories and products by unit
    /// count.
    /// </summary>
    Task<TelegramPantrySummary> GetPantrySummaryAsync(
        Guid hogarId,
        int categoryLimit,
        int productLimit,
        CancellationToken ct);

    /// <summary>
    /// Returns the household shopping-list items that are still pending:
    /// <c>RemovidoDeListaAt == null</c>, <c>Comprado</c> not true, and
    /// <c>AgregadoAlInventario</c> not true. The result is bounded to
    /// <paramref name="limit"/> items and exposes an overflow signal.
    /// </summary>
    Task<TelegramShoppingReadResult> GetPendingShoppingItemsAsync(
        Guid hogarId,
        int limit,
        CancellationToken ct);

    /// <summary>
    /// Returns household tasks that are assigned to the linked user and
    /// whose <c>Estado</c> is not <c>completada</c>. The result is bounded
    /// to <paramref name="limit"/> items and exposes an overflow signal.
    /// </summary>
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

/// <summary>
/// Single shopping-list item rendered for option 3. <see cref="GroupName"/>
/// is null when the item belongs to the manual/default group.
/// </summary>
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

/// <summary>
/// Single pending task rendered for option 4. <see cref="Status"/> and
/// <see cref="DueDate"/> are null when not set; the provider decides how
/// to surface them in the Telegram message.
/// </summary>
public sealed record TelegramPendingTaskItem(
    string Title,
    string? Status,
    DateOnly? DueDate);

public sealed record TelegramPendingTasksReadResult(
    IReadOnlyList<TelegramPendingTaskItem> Items,
    bool HasMore,
    int RemainingCount);
