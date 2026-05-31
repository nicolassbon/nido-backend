using Nido.Domain.Productos;
using Nido.Infrastructure.Persistence;
using ProductoEntity = Nido.Infrastructure.Persistence.Entities.Producto;

namespace Nido.Infrastructure.Productos;

public sealed class ProductRepository : IProductRepository
{
    private readonly NidoDbContext _db;

    public ProductRepository(NidoDbContext db)
    {
        _db = db;
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