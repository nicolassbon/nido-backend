using Nido.Domain.Productos;
using Nido.Infrastructure.Persistence;
using Nido.Application.Productos;
using ProductoEntity = Nido.Infrastructure.Persistence.Entities.Producto;
using Microsoft.EntityFrameworkCore;



namespace Nido.Infrastructure.Productos;

public sealed class ProductRepository : IProductRepository, IProductManualRepository{
    private readonly NidoDbContext _db;

    public ProductRepository(NidoDbContext db)
    {
        _db = db;
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
            stock.Producto.ImagenUrl,
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


    public async Task SaveAsync(
        Producto producto,
        CancellationToken cancellationToken)
    {
        var entity = new ProductoEntity
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            CodigoBarras = producto.CodigoBarras,
            ImagenUrl = producto.ImagenUrl,
            CategoriaId = producto.CategoriaId
        };

        await _db.Productos.AddAsync(entity, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}