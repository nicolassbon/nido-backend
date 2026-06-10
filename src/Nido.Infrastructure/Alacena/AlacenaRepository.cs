using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Nido.Application.Alacena;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Alacena;

public sealed class AlacenaRepository : IAlacenaRepository
{
    private readonly NidoDbContext _db;

    public AlacenaRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
    {
        return await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .Select(stock => ToResult(stock, stock.Producto))
            .ToListAsync(ct);
    }

    public async Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.Id == id && stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : ToResult(item, item.Producto);
    }

    public async Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
    {
        Producto? producto = null;
        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            producto = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == request.CodigoBarras, ct);
        }

        if (producto is null)
        {
            producto = new Producto
            {
                Id = Guid.NewGuid(),
                Nombre = request.Nombre,
                CodigoBarras = request.CodigoBarras,
                ImagenUrl = request.Imagen
            };
            _db.Productos.Add(producto);
        }
        else if (string.IsNullOrWhiteSpace(producto.ImagenUrl) && !string.IsNullOrWhiteSpace(request.Imagen))
        {
            producto.ImagenUrl = request.Imagen;
        }

        DateOnly? fechaVencimiento = null;
        if (!string.IsNullOrWhiteSpace(request.FechaVencimiento) && DateOnly.TryParse(request.FechaVencimiento, out var parsed))
        {
            fechaVencimiento = parsed;
        }

        var stock = new Nido.Infrastructure.Persistence.Entities.StockHogar
        {
            Id = Guid.NewGuid(),
            HogarId = request.HogarId,
            Producto = producto,
            CargadoPor = request.UsuarioId,
            UpdatedBy = request.UsuarioId,
            CantidadActual = request.Cantidad,
            UnidadMedida = NormalizeUnit(request.UnidadMedida),
            FechaVencimiento = fechaVencimiento,
            Ubicacion = request.Ubicacion,
            EstaAbierto = request.EstaAbierto,
            PorcentajeConsumido = request.PorcentajeConsumido,
            CantidadEnvases = request.CantidadEnvases < 1 ? 1 : request.CantidadEnvases
        };

        _db.StockHogars.Add(stock);
        await _db.SaveChangesAsync(ct);

        return ToResult(stock, producto);
    }

    public async Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .FirstOrDefaultAsync(stock => stock.Id == request.Id, ct);

        if (item is null)
        {
            return null;
        }

        if (request.Cantidad.HasValue)
            item.CantidadActual = request.Cantidad.Value;
        if (!string.IsNullOrWhiteSpace(request.Nombre))
            await UpdateProductReferenceAsync(item, request.Nombre, ct);
        if (request.Ubicacion is not null)
            item.Ubicacion = request.Ubicacion;
        if (request.UnidadMedida is not null)
            item.UnidadMedida = NormalizeUnit(request.UnidadMedida);
        if (request.EstaAbierto.HasValue)
            item.EstaAbierto = request.EstaAbierto.Value;
        if (request.PorcentajeConsumido.HasValue)
            item.PorcentajeConsumido = request.PorcentajeConsumido.Value;
        if (request.CantidadEnvases.HasValue)
            item.CantidadEnvases = request.CantidadEnvases.Value < 1 ? 1 : request.CantidadEnvases.Value;
        if (request.FechaVencimiento is not null)
            item.FechaVencimiento = DateOnly.TryParse(request.FechaVencimiento, out var parsed) ? parsed : null;

        item.UpdatedBy = request.UsuarioId;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToResult(item, item.Producto);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await _db.StockHogars.FindAsync([id], ct);
        if (item is null)
        {
            return false;
        }

        _db.StockHogars.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static StockItemResult ToResult(Nido.Infrastructure.Persistence.Entities.StockHogar stock, Producto producto)
        => new(
            stock.Id,
            stock.ProductoId,
            producto.Nombre,
            producto.ImagenUrl,
            producto.CodigoBarras,
            producto.Categoria?.Nombre,
            stock.Ubicacion,
            stock.CantidadActual ?? 0,
            stock.UnidadMedida,
            stock.FechaVencimiento?.ToString("yyyy-MM-dd"),
            stock.EstaAbierto,
            stock.PorcentajeConsumido,
            stock.CantidadEnvases);

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit) ? "unidad" : unit.Trim();

    private async Task UpdateProductReferenceAsync(Nido.Infrastructure.Persistence.Entities.StockHogar item, string nombre, CancellationToken ct)
    {
        var normalizedName = nombre.Trim();

        if (string.Equals(item.Producto.Nombre, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existingProducts = await _db.Productos
            .Include(producto => producto.Categoria)
            .ToListAsync(ct);
        var normalizedLookupName = NormalizeName(normalizedName);
        var existingProduct = existingProducts
            .FirstOrDefault(producto => NormalizeName(producto.Nombre) == normalizedLookupName);

        if (existingProduct is not null)
        {
            item.Producto = existingProduct;
            item.ProductoId = existingProduct.Id;
            return;
        }

        var newProduct = new Producto
        {
            Id = Guid.NewGuid(),
            Nombre = normalizedName,
            CategoriaId = item.Producto.CategoriaId,
            Categoria = item.Producto.Categoria
        };

        _db.Productos.Add(newProduct);
        item.Producto = newProduct;
        item.ProductoId = newProduct.Id;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
