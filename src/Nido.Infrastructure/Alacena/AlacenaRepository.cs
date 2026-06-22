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
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.InfoNutricionalProductos)
            .ThenInclude(info => info.Detalles)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.InfoNutricionalProductos)
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
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.InfoNutricionalProductos)
            .ThenInclude(info => info.Detalles)
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.InfoNutricionalProductos)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : ToResult(item, item.Producto);
    }

    public async Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
    {
        Producto? producto = null;
        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            producto = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == request.CodigoBarras, ct);
        }

        var isNewProducto = producto is null;

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
                CategoriaId = request.CategoriaId is null || request.CategoriaId == Guid.Empty
                    ? null
                    : request.CategoriaId,
                CodigoBarras = request.CodigoBarras,
                ImagenUrl = request.Imagen
            };
            _db.Productos.Add(producto);
        }
        else
        {
            if (producto.CategoriaId is null && request.CategoriaId is not null && request.CategoriaId != Guid.Empty)
                producto.CategoriaId = request.CategoriaId;
            if (string.IsNullOrWhiteSpace(producto.ImagenUrl) && !string.IsNullOrWhiteSpace(request.Imagen))
                producto.ImagenUrl = request.Imagen;
        }

        // Información nutricional (del escaneo). Se guarda a nivel Producto.
        // Para un producto existente, sólo se completa si todavía no la tiene.
        var hasNutrition = HasNutrition(request);
        if (hasNutrition)
        {
            var alreadyHasNutrition = !isNewProducto
                && await _db.Set<InfoNutricionalProducto>().AnyAsync(n => n.ProductoId == producto.Id, ct);
            if (!alreadyHasNutrition)
            {
                var nutri = BuildNutrition(producto.Id, request);
                _db.Set<InfoNutricionalProducto>().Add(nutri);
                producto.InfoNutricionalProductos.Add(nutri);
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
            .Include(stock => stock.Producto)
            .ThenInclude(producto => producto.InfoNutricionalProductos)
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
    {
        var nutri = producto.InfoNutricionalProductos?.FirstOrDefault();
        return new(
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
            ToNutritionResult(producto.InfoNutricionalProductos?.FirstOrDefault()));
    }

    private static NutritionInfoResult? ToNutritionResult(InfoNutricionalProducto? info)
    {
        if (info is null)
        {
            return null;
        }

        var items = info.Detalles
            .OrderBy(detalle => detalle.Orden)
            .ThenBy(detalle => detalle.Nombre)
            .Select(detalle => new NutritionInfoItemResult(
                detalle.Nombre,
                detalle.Valor,
                detalle.Unidad,
                detalle.PorcentajeDiario,
                detalle.Orden))
            .ToArray();

        if (items.Length == 0)
        {
            items = MacroItems(info.Calorias, info.Proteinas, info.Carbohidratos, info.Grasas);
        }

        return new NutritionInfoResult(
            info.Calorias,
            info.Proteinas,
            info.Carbohidratos,
            info.Grasas,
            info.Porcion,
            info.Base,
            items);
    }

    private static bool HasNutrition(CreateStockItemRequestModel request)
    {
        var info = request.InformacionNutricional;
        return request.Calorias.HasValue
            || request.Proteinas.HasValue
            || request.Carbohidratos.HasValue
            || request.Grasas.HasValue
            || info?.Calorias is not null
            || info?.Proteinas is not null
            || info?.Carbohidratos is not null
            || info?.Grasas is not null
            || !string.IsNullOrWhiteSpace(info?.Porcion)
            || !string.IsNullOrWhiteSpace(info?.Base)
            || (info?.Items.Any(item => !string.IsNullOrWhiteSpace(item.Nombre)) ?? false);
    }

    private static InfoNutricionalProducto BuildNutrition(Guid productoId, CreateStockItemRequestModel request)
    {
        var info = request.InformacionNutricional;
        var nutri = new InfoNutricionalProducto
        {
            Id = Guid.NewGuid(),
            ProductoId = productoId,
            Calorias = info?.Calorias ?? request.Calorias,
            Proteinas = info?.Proteinas ?? request.Proteinas,
            Carbohidratos = info?.Carbohidratos ?? request.Carbohidratos,
            Grasas = info?.Grasas ?? request.Grasas,
            Porcion = NormalizeText(info?.Porcion, 100),
            Base = NormalizeText(info?.Base, 100)
        };

        var details = info?.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .OrderBy(item => item.Orden)
            .Select((item, index) => new InfoNutricionalProductoDetalle
            {
                Id = Guid.NewGuid(),
                InfoNutricionalProductoId = nutri.Id,
                Nombre = NormalizeText(item.Nombre, 150) ?? string.Empty,
                Valor = item.Valor,
                Unidad = NormalizeText(item.Unidad, 30),
                PorcentajeDiario = item.PorcentajeDiario,
                Orden = item.Orden > 0 ? item.Orden : index + 1
            })
            .ToList() ?? new List<InfoNutricionalProductoDetalle>();

        if (details.Count == 0)
        {
            details = MacroItems(nutri.Calorias, nutri.Proteinas, nutri.Carbohidratos, nutri.Grasas)
                .Select(item => new InfoNutricionalProductoDetalle
                {
                    Id = Guid.NewGuid(),
                    InfoNutricionalProductoId = nutri.Id,
                    Nombre = item.Nombre,
                    Valor = item.Valor,
                    Unidad = item.Unidad,
                    PorcentajeDiario = item.PorcentajeDiario,
                    Orden = item.Orden
                })
                .ToList();
        }

        foreach (var detail in details)
        {
            nutri.Detalles.Add(detail);
        }

        return nutri;
    }

    private static NutritionInfoItemResult[] MacroItems(
        decimal? calorias,
        decimal? proteinas,
        decimal? carbohidratos,
        decimal? grasas)
    {
        var items = new List<NutritionInfoItemResult>();
        AddMacro(items, "Valor energetico", calorias, "kcal");
        AddMacro(items, "Proteinas", proteinas, "g");
        AddMacro(items, "Carbohidratos", carbohidratos, "g");
        AddMacro(items, "Grasas", grasas, "g");
        return items.ToArray();
    }

    private static void AddMacro(List<NutritionInfoItemResult> items, string nombre, decimal? valor, string unidad)
    {
        if (!valor.HasValue)
        {
            return;
        }

        items.Add(new NutritionInfoItemResult(nombre, valor, unidad, null, items.Count + 1));
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit) ? "unidad" : unit.Trim();

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
