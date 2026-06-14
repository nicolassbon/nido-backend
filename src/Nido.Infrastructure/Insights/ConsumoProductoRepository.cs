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
}
