using Microsoft.EntityFrameworkCore;
using Nido.Application.Productos;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Productos;

public sealed class ProductoRepository : IProductoRepository
{
    private readonly NidoDbContext _db;

    public ProductoRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, CancellationToken ct)
    {
        return await _db.Productos
            .AsNoTracking()
            .Include(producto => producto.Categoria)
            .Where(producto => producto.CodigoBarras == barcode)
            .Select(producto => new GetProductByBarcodeResult(
                producto.Id,
                producto.Nombre,
                producto.CodigoBarras,
                producto.ImagenUrl,
                producto.Categoria != null ? producto.Categoria.Nombre : null,
                producto.Categoria != null ? producto.Categoria.TtlDias : null))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct)
    {
        var normalizedName = nombre.Trim();

        return await _db.Productos
            .AsNoTracking()
            .Where(producto => EF.Functions.ILike(producto.Nombre, normalizedName))
            .Select(producto => new GetProductByNameResult(
                producto.Id,
                producto.Nombre,
                producto.CategoriaId,
                producto.ImagenUrl))
            .FirstOrDefaultAsync(ct);
    }
}
