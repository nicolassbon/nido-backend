using Microsoft.EntityFrameworkCore;
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
            .Select(stock => ToResult(stock, stock.Producto))
            .ToListAsync(ct);
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

        var stock = new StockHogar
        {
            Id = Guid.NewGuid(),
            HogarId = request.HogarId,
            Producto = producto,
            CargadoPor = request.UsuarioId,
            UpdatedBy = request.UsuarioId,
            CantidadActual = request.Cantidad,
            UnidadMedida = "unidad",
            FechaVencimiento = fechaVencimiento,
            Ubicacion = request.Ubicacion,
            EstaAbierto = request.EstaAbierto,
            PorcentajeConsumido = request.PorcentajeConsumido
        };

        _db.StockHogars.Add(stock);
        await _db.SaveChangesAsync(ct);

        return ToResult(stock, producto);
    }

    public async Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .Include(stock => stock.Producto)
            .FirstOrDefaultAsync(stock => stock.Id == request.Id, ct);

        if (item is null)
        {
            return null;
        }

        if (request.Cantidad.HasValue)
            item.CantidadActual = request.Cantidad.Value;
        if (request.Ubicacion is not null)
            item.Ubicacion = request.Ubicacion;
        if (request.EstaAbierto.HasValue)
            item.EstaAbierto = request.EstaAbierto.Value;
        if (request.PorcentajeConsumido.HasValue)
            item.PorcentajeConsumido = request.PorcentajeConsumido.Value;
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

    private static StockItemResult ToResult(StockHogar stock, Producto producto)
        => new(
            stock.Id,
            stock.ProductoId,
            producto.Nombre,
            producto.ImagenUrl,
            producto.CodigoBarras,
            stock.Ubicacion,
            stock.CantidadActual ?? 0,
            stock.FechaVencimiento?.ToString("yyyy-MM-dd"),
            stock.EstaAbierto,
            stock.PorcentajeConsumido);
}
