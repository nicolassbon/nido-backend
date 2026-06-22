using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Nido.Application.Alacena;
using Nido.Application.Common.Assets;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Alacena;

public sealed class AlacenaRepository : IAlacenaRepository
{
    private readonly NidoDbContext _db;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public AlacenaRepository(NidoDbContext db, IPublicAssetUrlResolver assetUrlResolver)
    {
        _db = db;
        _assetUrlResolver = assetUrlResolver;
    }

    public async Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
    {
        var items = await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .ToListAsync(ct);

        return items.Select(stock => ToResult(stock, stock.Producto)).ToList();
    }

    public async Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.Id == id && stock.HogarId == hogarId)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : ToResult(item, item.Producto);
    }

    public async Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
    {
        var resolvedCategoriaId = await ResolveCategoryIdAsync(request.CategoriaId, request.Nombre, ct);
        Producto? producto = null;
        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            producto = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == request.CodigoBarras, ct);
        }

        if (producto is null)
        {
            var normalizedName = NormalizeName(request.Nombre);
            var productos = await _db.Productos.ToListAsync(ct);
            producto = productos.FirstOrDefault(p => NormalizeName(p.Nombre) == normalizedName);
        }

        if (producto is null)
        {
            producto = new Producto
            {
                Id = Guid.NewGuid(),
                Nombre = request.Nombre,
                CategoriaId = resolvedCategoriaId,
                CodigoBarras = request.CodigoBarras,
                ImagenUrl = request.Imagen
            };
            _db.Productos.Add(producto);
        }
        else
        {
            if (request.CategoriaId is not null && request.CategoriaId != Guid.Empty && producto.CategoriaId != request.CategoriaId)
            {
                producto.CategoriaId = request.CategoriaId;
            }
            else if (producto.CategoriaId is null && resolvedCategoriaId is not null)
            {
                producto.CategoriaId = resolvedCategoriaId;
            }

            if (string.IsNullOrWhiteSpace(producto.ImagenUrl) && !string.IsNullOrWhiteSpace(request.Imagen))
            {
                producto.ImagenUrl = request.Imagen;
            }
        }

        DateOnly? fechaVencimiento = null;
        if (!string.IsNullOrWhiteSpace(request.FechaVencimiento) && DateOnly.TryParse(request.FechaVencimiento, out var parsed))
        {
            fechaVencimiento = parsed;
        }

        var stock = new Nido.Infrastructure.Persistence.Entities.StockHogar
        {
            Id = Guid.NewGuid(),
            HogarId = request.HogarId,
            Producto = producto,
            CargadoPor = request.UsuarioId,
            UpdatedBy = request.UsuarioId,
            CantidadActual = request.Cantidad,
            UnidadMedida = NormalizeUnit(request.UnidadMedida),
            FechaVencimiento = fechaVencimiento,
            Ubicacion = request.Ubicacion,
            EstaAbierto = request.EstaAbierto,
            PorcentajeConsumido = request.PorcentajeConsumido,
            CantidadEnvases = request.CantidadEnvases < 1 ? 1 : request.CantidadEnvases,
            OrigenCarga = request.OrigenCarga
        };

        _db.StockHogars.Add(stock);
        await _db.SaveChangesAsync(ct);

        return ToResult(stock, producto);
    }

    public async Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.Categoria)
            .FirstOrDefaultAsync(stock => stock.Id == request.Id && stock.HogarId == request.HogarId, ct);

        if (item is null)
        {
            return null;
        }

        if (request.Cantidad.HasValue)
            item.CantidadActual = request.Cantidad.Value;
        if (!string.IsNullOrWhiteSpace(request.Nombre))
            await UpdateProductReferenceAsync(item, request.Nombre, ct);
        if (request.Ubicacion is not null)
            item.Ubicacion = request.Ubicacion;
        if (request.UnidadMedida is not null)
            item.UnidadMedida = NormalizeUnit(request.UnidadMedida);
        if (request.EstaAbierto.HasValue)
            item.EstaAbierto = request.EstaAbierto.Value;
        if (request.PorcentajeConsumido.HasValue)
            item.PorcentajeConsumido = request.PorcentajeConsumido.Value;
        if (request.CantidadEnvases.HasValue)
            item.CantidadEnvases = request.CantidadEnvases.Value < 1 ? 1 : request.CantidadEnvases.Value;
        if (request.FechaVencimiento is not null)
            item.FechaVencimiento = DateOnly.TryParse(request.FechaVencimiento, out var parsed) ? parsed : null;

        item.UpdatedBy = request.UsuarioId;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToResult(item, item.Producto);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var item = await _db.StockHogars
            .FirstOrDefaultAsync(stock => stock.Id == id && stock.HogarId == hogarId, ct);

        if (item is null)
        {
            return false;
        }

        _db.StockHogars.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private StockItemResult ToResult(Nido.Infrastructure.Persistence.Entities.StockHogar stock, Producto producto)
        => new(
            stock.Id,
            stock.ProductoId,
            producto.Nombre,
            _assetUrlResolver.Resolve(producto.ImagenUrl),
            producto.CodigoBarras,
            producto.Categoria?.Nombre,
            stock.Ubicacion,
            stock.CantidadActual ?? 0,
            stock.UnidadMedida,
            stock.FechaVencimiento?.ToString("yyyy-MM-dd"),
            stock.EstaAbierto,
            stock.PorcentajeConsumido,
            stock.CantidadEnvases,
            string.IsNullOrWhiteSpace(stock.OrigenCarga) ? StockLoadOrigins.Manual : stock.OrigenCarga,
            producto.Categoria?.IconoSvg,
            producto.Categoria?.Icono,
            producto.CantidadCompraEstandar,
            producto.UnidadCompraEstandar);

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit) ? "unidad" : unit.Trim();

    private async Task<Guid?> ResolveCategoryIdAsync(Guid? requestedCategoryId, string productName, CancellationToken ct)
    {
        if (requestedCategoryId is not null && requestedCategoryId != Guid.Empty)
        {
            return requestedCategoryId;
        }

        var categoryName = InferCategoryNameByKeyword(NormalizeName(productName));
        if (categoryName is null)
        {
            return null;
        }

        return await _db.CategoriasProductos
            .AsNoTracking()
            .Where(categoria => categoria.Nombre == categoryName)
            .Select(categoria => (Guid?)categoria.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static string? InferCategoryNameByKeyword(string normalizedName)
    {
        if (ContainsAny(normalizedName, "leche", "queso", "yogur", "crema", "manteca", "lacteo", "ricota", "dulce de leche"))
            return "Lácteos";
        if (ContainsAny(normalizedName, "pollo", "ave", "gallina"))
            return "Pollo y Aves";
        if (ContainsAny(normalizedName, "cerdo", "bondiola", "panceta", "jamon", "chorizo", "salchicha"))
            return "Carnes Porcinas";
        if (ContainsAny(normalizedName, "pescado", "atun", "merluza", "marisco", "camaron", "langostino"))
            return "Pescados y Mariscos";
        if (ContainsAny(normalizedName, "carne", "vaca", "bife", "milanesa", "asado", "lomo", "nalga", "cuadril"))
            return "Carnes Vacunas";
        if (ContainsAny(normalizedName, "manzana", "banana", "naranja", "limon", "frutilla", "uva", "pera", "durazno", "cereza", "fruta"))
            return "Frutas";
        if (ContainsAny(normalizedName, "ajo en polvo", "cebolla en polvo", "pimenton", "aji molido"))
            return "Condimentos";
        if (ContainsAny(normalizedName, "tomate", "zanahoria", "cebolla", "lechuga", "papa", "batata", "morron", "ajo", "verdura", "zapallo", "espinaca", "acelga"))
            return "Verduras";
        if (ContainsAny(normalizedName, "lenteja", "garbanzo", "poroto", "arveja", "legumbre", "soja"))
            return "Legumbres";
        if (ContainsAny(normalizedName, "pan", "factura", "medialuna", "tostada", "galleta de agua"))
            return "Panificados";
        if (ContainsAny(normalizedName, "fideo", "pasta", "spaghetti", "raviol", "ñoqui", "noqui"))
            return "Pastas";
        if (ContainsAny(normalizedName, "arroz"))
            return "Arroz";
        if (ContainsAny(normalizedName, "cereal", "avena", "granola", "copos"))
            return "Cereales";
        if (ContainsAny(normalizedName, "harina", "maicena", "fecula"))
            return "Harinas";
        if (ContainsAny(normalizedName, "azucar", "edulcorante", "miel", "endulzante"))
            return "Azúcar y Endulzantes";
        if (ContainsAny(normalizedName, "chocolate", "levadura", "esencia", "reposteria", "polvo de hornear", "coco rallado"))
            return "Repostería";
        if (ContainsAny(normalizedName, "aceite"))
            return "Aceites";
        if (ContainsAny(normalizedName, "sal", "pimienta", "oregano", "condimento", "especia", "caldo"))
            return "Condimentos";
        if (ContainsAny(normalizedName, "salsa", "aderezo", "mayonesa", "ketchup", "mostaza", "vinagre", "aceto"))
            return "Salsas y Aderezos";
        if (ContainsAny(normalizedName, "huevo"))
            return "Huevos";
        if (ContainsAny(normalizedName, "congelado", "hielo", "helado", "freezer"))
            return "Congelados";
        if (ContainsAny(normalizedName, "papas fritas", "snack", "nachos", "chips", "mani"))
            return "Snacks";
        if (ContainsAny(normalizedName, "caramelo", "gomita", "alfajor", "golosina", "turron"))
            return "Golosinas";
        if (ContainsAny(normalizedName, "cerveza", "vino", "fernet", "sidra", "whisky", "alcoholica"))
            return "Bebidas Alcohólicas";
        if (ContainsAny(normalizedName, "agua", "jugo", "gaseosa", "soda", "coca", "bebida", "te", "cafe"))
            return "Bebidas";
        if (ContainsAny(normalizedName, "conserva", "lata", "atun", "tomate perita", "mermelada"))
            return "Conservas";
        if (ContainsAny(normalizedName, "diet", "light", "proteina", "dietetico"))
            return "Productos Dietéticos";
        if (ContainsAny(normalizedName, "sin tacc", "gluten free", "gluten-free", "celiaco"))
            return "Productos Sin TACC";
        if (ContainsAny(normalizedName, "detergente", "jabon", "limpieza", "lavandina", "desinfectante", "trapo", "esponja", "suavizante"))
            return "Limpieza";
        if (ContainsAny(normalizedName, "shampoo", "acondicionador", "papel higienico", "dentifrico", "desodorante", "baño", "bano", "jabon de tocador"))
            return "Higiene Personal";
        if (ContainsAny(normalizedName, "perro", "gato", "mascota", "alimento balanceado"))
            return "Mascotas";
        if (ContainsAny(normalizedName, "pañal", "panal", "bebe", "mamadera", "oleo calcareo"))
            return "Bebés";

        return "Otros";
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task UpdateProductReferenceAsync(Nido.Infrastructure.Persistence.Entities.StockHogar item, string nombre, CancellationToken ct)
    {
        var normalizedName = nombre.Trim();

        if (string.Equals(item.Producto.Nombre, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existingProducts = await _db.Productos
            .Include(producto => producto.Categoria)
            .ToListAsync(ct);
        var normalizedLookupName = NormalizeName(normalizedName);
        var existingProduct = existingProducts
            .FirstOrDefault(producto => NormalizeName(producto.Nombre) == normalizedLookupName);

        if (existingProduct is not null)
        {
            item.Producto = existingProduct;
            item.ProductoId = existingProduct.Id;
            return;
        }

        var newProduct = new Producto
        {
            Id = Guid.NewGuid(),
            Nombre = normalizedName,
            CategoriaId = item.Producto.CategoriaId,
            Categoria = item.Producto.Categoria
        };

        _db.Productos.Add(newProduct);
        item.Producto = newProduct;
        item.ProductoId = newProduct.Id;
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
}
