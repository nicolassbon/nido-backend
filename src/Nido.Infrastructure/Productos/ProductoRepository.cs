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

    public async Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, Guid? hogarId, CancellationToken ct)
    {
        var producto = await _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.CodigoBarras == barcode, ct);

        if (producto is null) return null;

        // Nutrición guardada del producto (si la tiene).
        var nutri = await _db.InfoNutricionalProductos
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductoId == producto.Id, ct);

        // Última compra de este producto en el hogar, para pre-llenar gramaje + unidad.
        decimal? gramaje = null;
        string? unidad = null;
        if (hogarId is not null)
        {
            var ultimaCompra = await _db.StockHogars
                .AsNoTracking()
                .Where(s => s.ProductoId == producto.Id && s.HogarId == hogarId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.CantidadActual, s.UnidadMedida })
                .FirstOrDefaultAsync(ct);

            if (ultimaCompra is not null)
            {
                gramaje = ultimaCompra.CantidadActual;
                unidad = ultimaCompra.UnidadMedida;
            }
        }

        return new GetProductByBarcodeResult(
            producto.Id,
            producto.Nombre,
            producto.CodigoBarras,
            _assetUrlResolver.Resolve(producto.ImagenUrl),
            producto.Categoria?.Nombre,
            producto.Categoria?.TtlDias,
            gramaje,
            unidad,
            nutri?.Calorias,
            nutri?.Proteinas,
            nutri?.Carbohidratos,
            nutri?.Grasas);
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

    public async Task<GetProductByNameResult> CreateAsync(
        string nombre,
        Guid? categoriaId,
        CancellationToken ct,
        decimal? calorias = null,
        decimal? proteinas = null,
        decimal? carbohidratos = null,
        decimal? grasas = null)
    {
        var nuevo = new Nido.Infrastructure.Persistence.Entities.Producto
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId == Guid.Empty ? null : categoriaId,
            ImagenUrl = null,
        };

        _db.Productos.Add(nuevo);

        // Información nutricional (del escaneo a Open Food Facts).
        if (calorias.HasValue || proteinas.HasValue || carbohidratos.HasValue || grasas.HasValue)
        {
            _db.Set<Nido.Infrastructure.Persistence.Entities.InfoNutricionalProducto>().Add(
                new Nido.Infrastructure.Persistence.Entities.InfoNutricionalProducto
                {
                    Id = Guid.NewGuid(),
                    ProductoId = nuevo.Id,
                    Calorias = calorias,
                    Proteinas = proteinas,
                    Carbohidratos = carbohidratos,
                    Grasas = grasas,
                });
        }

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
