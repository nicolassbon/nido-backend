using Nido.Infrastructure.Persistence;
using Nido.Application.Productos;


using StockEntity = Nido.Infrastructure.Persistence.Entities.StockHogar;
using StockDomain = Nido.Domain.StockHogar.StockHogar;

namespace Nido.Infrastructure.StockHogar;

public sealed class StockHogarRepository : IStockHogarRepository
{
    private readonly NidoDbContext _db;

    public StockHogarRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(
        StockDomain stockHogar,
        CancellationToken cancellationToken)
    {
        var entity = new StockEntity
        {
            Id = stockHogar.Id,
            HogarId = stockHogar.HogarId,
            ProductoId = stockHogar.ProductoId,

            CantidadActual = stockHogar.CantidadActual,
            UnidadMedida = stockHogar.UnidadMedida,

            CargadoPor = stockHogar.UsuarioIngresoId,
            UpdatedBy = stockHogar.UsuarioIngresoId,

            FechaVencimiento = stockHogar.FechaVencimiento.HasValue
                ? DateOnly.FromDateTime(stockHogar.FechaVencimiento.Value)
                : null,

            Ubicacion = "Alacena",
            EstaAbierto = false,
            PorcentajeConsumido = 0
        };

        await _db.Set<StockEntity>()
            .AddAsync(entity, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}