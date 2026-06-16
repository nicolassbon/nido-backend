using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Nido.Application.Common.Assets;
using Nido.Application.Productos;
using Nido.Application.Productos.UploadProductImage;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Productos;

public sealed class ProductoRepository : IProductoRepository, IProductImageRepository
{
    private readonly NidoDbContext _db;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public ProductoRepository(NidoDbContext db, IPublicAssetUrlResolver assetUrlResolver)
    {
        _db = db;
        _assetUrlResolver = assetUrlResolver;
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
                _assetUrlResolver.Resolve(producto.ImagenUrl),
                producto.Categoria != null ? producto.Categoria.Nombre : null,
                producto.Categoria != null ? producto.Categoria.TtlDias : null))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct)
    {
        var normalizedName = NormalizeName(nombre);

        var exactMatch = await _db.Productos
            .AsNoTracking()
            .Where(producto => EF.Functions.ILike(producto.Nombre, nombre.Trim()))
            .Select(producto => new GetProductByNameResult(
                producto.Id,
                producto.Nombre,
                producto.CategoriaId,
                _assetUrlResolver.Resolve(producto.ImagenUrl)))
            .FirstOrDefaultAsync(ct);

        if (exactMatch is not null)
            return exactMatch;

        var products = await _db.Productos
            .AsNoTracking()
            .Select(producto => new GetProductByNameResult(
                producto.Id,
                producto.Nombre,
                producto.CategoriaId,
                _assetUrlResolver.Resolve(producto.ImagenUrl)))
            .ToListAsync(ct);

        return products.FirstOrDefault(producto => NormalizeName(producto.Nombre) == normalizedName);
    }

    public async Task<GetProductByNameResult> CreateAsync(string nombre, Guid? categoriaId, CancellationToken ct)
    {
        var nuevo = new Nido.Infrastructure.Persistence.Entities.Producto
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId == Guid.Empty ? null : categoriaId,
            ImagenUrl = null,
        };

        _db.Productos.Add(nuevo);
        await _db.SaveChangesAsync(ct);

        return new GetProductByNameResult(nuevo.Id, nuevo.Nombre, nuevo.CategoriaId, nuevo.ImagenUrl);
    }

    public async Task<IEnumerable<SearchProductosResult>> SearchByNombreAsync(string query, CancellationToken ct)
    {
        var pattern = $"%{query.Trim()}%";

        return await _db.Productos
            .AsNoTracking()
            .Where(p => EF.Functions.ILike(p.Nombre, pattern))
            .OrderBy(p => p.Nombre)
            .Take(10)
            .Select(p => new SearchProductosResult(
                p.Nombre,
                p.Categoria != null ? p.Categoria.Nombre : null,
                p.CategoriaId,
                p.StockHogars.OrderByDescending(s => s.CreatedAt).Select(s => s.UnidadMedida).FirstOrDefault(),
                p.StockHogars.OrderByDescending(s => s.CreatedAt).Select(s => s.Ubicacion).FirstOrDefault()
            ))
            .ToListAsync(ct);
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

    public async Task<ProductImageTarget?> GetImageTargetAsync(Guid productId, Guid hogarId, CancellationToken cancellationToken)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(x => x.Id == productId && x.StockHogars.Any(stock => stock.HogarId == hogarId))
            .Select(x => new ProductImageTarget(x.Id, x.ImagenUrl))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateImageKeyAsync(Guid productId, Guid hogarId, string storageKey, CancellationToken cancellationToken)
    {
        var producto = await _db.Productos
            .Where(x => x.Id == productId && x.StockHogars.Any(stock => stock.HogarId == hogarId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ProductImageTargetNotFoundException();

        producto.ImagenUrl = storageKey;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
