using Microsoft.EntityFrameworkCore;
using Nido.Application.ListaCompras;
using Nido.Application.Telegram.Menu;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Telegram.Menu;

public sealed class EfTelegramMenuReadService(NidoDbContext db) : ITelegramMenuReadService
{
    public async Task<TelegramExpiringStockReadResult> GetExpiringStockAsync(
        Guid hogarId,
        DateOnly today,
        int days,
        int limit,
        CancellationToken ct)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        var limitDate = today.AddDays(days);

        var rows = await db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId
                && stock.FechaVencimiento.HasValue
                && stock.FechaVencimiento.Value >= today
                && stock.FechaVencimiento.Value <= limitDate)
            .Include(stock => stock.Producto)
            .OrderBy(stock => stock.FechaVencimiento)
            .ThenBy(stock => stock.Producto.Nombre)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var visible = hasMore ? rows.Take(limit).ToList() : rows;

        var items = visible
            .Select(stock => new TelegramExpiringStockItem(
                stock.Producto.Nombre,
                stock.CantidadActual,
                stock.UnidadMedida,
                stock.CantidadEnvases,
                stock.FechaVencimiento!.Value))
            .ToList();

        return new TelegramExpiringStockReadResult(items, hasMore, Math.Max(0, rows.Count - limit));
    }

    public async Task<TelegramPantrySummary> GetPantrySummaryAsync(
        Guid hogarId,
        int categoryLimit,
        int productLimit,
        CancellationToken ct)
    {
        if (categoryLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryLimit), "Category limit must be greater than zero.");
        }

        if (productLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productLimit), "Product limit must be greater than zero.");
        }

        var items = await db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
                .ThenInclude(producto => producto.Categoria)
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            return new TelegramPantrySummary(0, 0, 0, Array.Empty<TelegramPantryLineCount>(), Array.Empty<TelegramPantryLineCount>());
        }

        var totalUnits = items.Sum(stock => Math.Max(stock.CantidadEnvases, 1));
        var distinctProducts = items.Select(stock => stock.ProductoId).Distinct().Count();
        var distinctCategoryCount = items
            .Select(stock => stock.Producto.Categoria?.Nombre ?? "Sin categoría")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var categories = items
            .GroupBy(stock => stock.Producto.Categoria?.Nombre ?? "Sin categoría")
            .Select(group => new TelegramPantryLineCount(
                group.Key,
                group.Sum(stock => Math.Max(stock.CantidadEnvases, 1))))
            .OrderByDescending(line => line.Count)
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .Take(categoryLimit)
            .ToList();

        var products = items
            .GroupBy(stock => stock.Producto.Nombre)
            .Select(group => new TelegramPantryLineCount(
                group.Key,
                group.Sum(stock => Math.Max(stock.CantidadEnvases, 1))))
            .OrderByDescending(line => line.Count)
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .Take(productLimit)
            .ToList();

        return new TelegramPantrySummary(totalUnits, distinctProducts, distinctCategoryCount, categories, products);
    }

    public async Task<TelegramShoppingReadResult> GetPendingShoppingItemsAsync(
        Guid hogarId,
        int limit,
        CancellationToken ct)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        var rows = await db.ListaCompras
            .AsNoTracking()
            .Where(item => item.HogarId == hogarId
                && item.RemovidoDeListaAt == null
                && item.Comprado != true
                && item.AgregadoAlInventario != true)
            .Include(item => item.Producto)
            .OrderBy(item => item.GrupoNombre)
            .ThenBy(item => item.Orden)
            .ThenBy(item => item.CreatedAt)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var visible = hasMore ? rows.Take(limit).ToList() : rows;

        var items = visible
            .Select(item => new TelegramShoppingItem(
                ResolveShoppingItemName(item),
                item.Cantidad,
                item.Unidad,
                ContainerCount: 1,
                ResolveGroupName(item.GrupoNombre)))
            .ToList();

        return new TelegramShoppingReadResult(items, hasMore, Math.Max(0, rows.Count - limit));
    }

    public async Task<TelegramPendingTasksReadResult> GetPendingAssignedTasksAsync(
        Guid hogarId,
        Guid usuarioId,
        int limit,
        CancellationToken ct)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        var rows = await db.Tareas
            .AsNoTracking()
            .Where(task => task.HogarId == hogarId
                && task.Estado != "completada"
                && task.AsignacionesTareas.Any(assignment => assignment.UsuarioId == usuarioId))
            .OrderBy(task => task.FechaLimite.HasValue ? 0 : 1)
            .ThenBy(task => task.FechaLimite)
            .ThenByDescending(task => task.CreatedAt)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var visible = hasMore ? rows.Take(limit).ToList() : rows;

        var items = visible
            .Select(task => new TelegramPendingTaskItem(
                task.Titulo,
                task.Estado,
                task.FechaLimite.HasValue ? DateOnly.FromDateTime(task.FechaLimite.Value) : (DateOnly?)null,
                task.Id))
            .ToList();

        return new TelegramPendingTasksReadResult(items, hasMore, Math.Max(0, rows.Count - limit));
    }

    private static string ResolveShoppingItemName(Persistence.Entities.ListaCompra item)
        => string.IsNullOrWhiteSpace(item.NombreManual)
            ? string.IsNullOrWhiteSpace(item.ProductoNombreSnapshot)
                ? item.Producto?.Nombre ?? "Producto sin nombre"
                : item.ProductoNombreSnapshot.Trim()
            : item.NombreManual.Trim();

    private static string? ResolveGroupName(string? grupoNombre)
    {
        if (string.IsNullOrWhiteSpace(grupoNombre) || grupoNombre == ListaComprasDefaults.ManualGroupName)
        {
            return null;
        }

        return grupoNombre.Trim();
    }
}
