using Nido.Application.Productos;
using Nido.Application.Common.Assets;
using Nido.Infrastructure.Persistence;
using ProductoEntity = Nido.Infrastructure.Persistence.Entities.Producto;
using Microsoft.EntityFrameworkCore;

namespace Nido.Infrastructure.Productos;

public sealed class ProductRepository : IProductManualRepository
{
    private readonly NidoDbContext _db;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public ProductRepository(NidoDbContext db, IPublicAssetUrlResolver assetUrlResolver)
    {
        _db = db;
        _assetUrlResolver = assetUrlResolver;
    }


    public async Task<IReadOnlyList<GetProductManualResult>> GetManualByHogarAsync(
        Guid hogarId,
        CancellationToken cancellationToken)
    {
        return await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
                .ThenInclude(producto => producto.Categoria)
            .OrderBy(stock => stock.FechaVencimiento)
            .Select(stock => new GetProductManualResult(
                stock.Id,
                stock.ProductoId,
                stock.Producto.Nombre,
                stock.Producto.CategoriaId,
                stock.Producto.Categoria != null ? stock.Producto.Categoria.Nombre : null,
                stock.Producto.CodigoBarras,
                _assetUrlResolver.Resolve(stock.Producto.ImagenUrl),
                stock.Ubicacion,
                stock.CantidadActual ?? 0,
                stock.UnidadMedida,
                stock.FechaVencimiento.HasValue
                    ? stock.FechaVencimiento.Value.ToString("yyyy-MM-dd")
                    : null,
                stock.EstaAbierto,
                stock.PorcentajeConsumido
            ))
            .ToListAsync(cancellationToken);
    }


}
