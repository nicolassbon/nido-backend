using Microsoft.EntityFrameworkCore;
using Nido.Application.Estadisticas;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Estadisticas;

public sealed class EstadisticasRepository : IEstadisticasRepository
{
    private readonly NidoDbContext _db;

    public EstadisticasRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StockSnapshot>> GetStockSnapshotAsync(Guid hogarId, CancellationToken ct)
    {
        return await _db.StockHogars
            .AsNoTracking()
            .Include(s => s.Producto)
                .ThenInclude(p => p.Categoria)
            .Where(s => s.HogarId == hogarId)
            .Select(s => new StockSnapshot(
                s.Id,
                s.ProductoId,
                s.Producto.Nombre,
                s.Producto.Categoria != null ? s.Producto.Categoria.Nombre : null,
                s.Ubicacion,
                s.CantidadActual ?? 0m,
                s.UnidadMedida,
                s.EstaAbierto,
                s.PorcentajeConsumido,
                s.FechaVencimiento,
                s.CreatedAt))
            .ToListAsync(ct);
    }
}
