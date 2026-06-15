using Microsoft.EntityFrameworkCore;
using Nido.Application.Insights;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Insights;

public sealed class ConsumoProductoRepository : IConsumoProductoRepository
{
    private readonly NidoDbContext _db;

    public ConsumoProductoRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(RegistrarConsumoInput input, CancellationToken ct)
    {
        var consumo = new ConsumoProducto
        {
            Id              = Guid.NewGuid(),
            HogarId         = input.HogarId,
            ProductoId      = input.ProductoId,
            ProductoNombre  = input.ProductoNombre.Trim(),
            CategoriaId     = input.CategoriaId,
            Cantidad        = input.Cantidad,
            UnidadMedida    = input.UnidadMedida,
            FechaConsumo    = DateTime.UtcNow,
            Motivo          = input.Motivo,
            UsuarioId       = input.UsuarioId,
        };

        _db.ConsumosProducto.Add(consumo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConsumoPorProducto>> GetConsumosPorProductoAsync(
        Guid hogarId, int diasAtras, CancellationToken ct)
    {
        var desde = DateTime.UtcNow.AddDays(-diasAtras);

        return await _db.ConsumosProducto
            .AsNoTracking()
            .Where(c => c.HogarId == hogarId && c.FechaConsumo >= desde)
            .GroupBy(c => new { c.ProductoId, c.ProductoNombre })
            .Select(g => new ConsumoPorProducto(
                g.Key.ProductoId,
                g.Key.ProductoNombre,
                g.Sum(x => x.Cantidad),
                g.Count(),
                g.Count(x => x.Motivo == ConsumoMotivos.Vencido),
                g.Count(x => x.Motivo == ConsumoMotivos.Cocinado),
                g.Max(x => x.FechaConsumo)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ComprasPorProducto>> GetComprasPorProductoAsync(
        Guid hogarId, int diasAtras, CancellationToken ct)
    {
        var desde = DateTime.UtcNow.AddDays(-diasAtras);

        var rows = await _db.StockHogars
            .AsNoTracking()
            .Where(s => s.HogarId == hogarId && s.CreatedAt >= desde)
            .Select(s => new
            {
                ProductoId = (Guid?)s.ProductoId,
                Nombre = s.Producto.Nombre,
                Fecha = s.CreatedAt,
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new { r.ProductoId, r.Nombre })
            .Select(g => new ComprasPorProducto(
                g.Key.ProductoId,
                g.Key.Nombre,
                g.OrderBy(x => x.Fecha).Select(x => x.Fecha).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<DayOfWeek, int>> GetCocinadasPorDiaSemanaAsync(
        Guid hogarId, int diasAtras, CancellationToken ct)
    {
        var desde = DateTime.UtcNow.AddDays(-diasAtras);

        var fechas = await _db.RecetasCocinadas
            .AsNoTracking()
            .Where(rc => rc.HogarId == hogarId && rc.Fecha >= desde)
            .Select(rc => rc.Fecha)
            .ToListAsync(ct);

        return fechas
            .GroupBy(f => f.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<IReadOnlyList<RecetaTopItem>> GetRecetasTopAsync(
        Guid hogarId, int diasAtras, int topN, CancellationToken ct)
    {
        var desde = DateTime.UtcNow.AddDays(-diasAtras);

        var rows = await _db.RecetasCocinadas
            .AsNoTracking()
            .Where(rc => rc.HogarId == hogarId && rc.Fecha >= desde)
            .Select(rc => new { rc.RecetaId, NombreReceta = rc.Receta.Nombre })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => new { x.RecetaId, x.NombreReceta })
            .Select(g => new RecetaTopItem(g.Key.RecetaId, g.Key.NombreReceta, g.Count()))
            .OrderByDescending(x => x.VecesCocinada)
            .Take(topN)
            .ToList();
    }

    public async Task<IReadOnlyList<EnvaseZombieRaw>> GetEnvasesAbiertosLargoAsync(
        Guid hogarId, int diasMinAbierto, CancellationToken ct)
    {
        var limite = DateTime.UtcNow.AddDays(-diasMinAbierto);

        return await _db.StockHogars
            .AsNoTracking()
            .Where(s => s.HogarId == hogarId
                     && s.EstaAbierto
                     && (s.UpdatedAt ?? s.CreatedAt) <= limite)
            .Select(s => new EnvaseZombieRaw(
                s.Id,
                s.Producto.Nombre,
                s.UpdatedAt ?? s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DateTime>> GetFechasComprasHogarAsync(
        Guid hogarId, int diasAtras, CancellationToken ct)
    {
        var desde = DateTime.UtcNow.AddDays(-diasAtras);
        return await _db.StockHogars
            .AsNoTracking()
            .Where(s => s.HogarId == hogarId && s.CreatedAt >= desde)
            .Select(s => s.CreatedAt)
            .ToListAsync(ct);
    }
}
