using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.Assets;
using Nido.Application.Recetas;
using Nido.Application.Recetas.UploadRecipeImage;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Recetas;

public sealed class RecetaRepository : IRecetaRepository, IRecipeImageRepository
{
    private static readonly IReadOnlyDictionary<string, string[]> AllergenAliases = new Dictionary<string, string[]>
    {
        ["Maní"] = ["mani", "cacahuate", "peanut", "peanuts"],
        ["Gluten"] = ["gluten", "harina", "trigo", "fideos", "pasta", "pan", "pan rallado", "masa", "avena"],
        ["Lactosa"] = ["lactosa", "leche", "leche en polvo", "queso", "queso crema", "crema", "manteca", "mantequilla", "yogur", "yogurt", "ricota", "mozzarella", "parmesano", "dulce de leche", "milk", "cheese", "butter", "cream"],
        ["Mariscos"] = ["mariscos", "camaron", "camarones", "langostino", "langostinos", "mejillon", "mejillones", "calamar", "calamares"],
        ["Soja"] = ["soja", "soya", "tofu", "salsa de soja"],
        ["Huevo"] = ["huevo", "huevos", "clara", "claras", "yema", "yemas", "egg", "eggs"],
        ["Frutos secos"] = ["frutos secos", "almendra", "almendras", "nuez", "nueces", "avellana", "avellanas", "castana", "castanas", "pistacho", "pistachos"],
        ["Pescado"] = ["pescado", "atun", "salmon", "merluza", "sardina", "sardinas"],
        ["Carne"] = ["carne", "pollo", "pechuga", "cerdo", "jamon", "panceta", "tocino", "chorizo", "salchicha", "vacuno", "vacuna", "res", "ternera", "cordero", "pavo", "beef", "chicken", "pork", "bacon", "ham"],
        ["Sésamo"] = ["sesamo", "tahini"],
        ["Mostaza"] = ["mostaza", "mustard"],
    };

    private static readonly HashSet<string> MatchStopWords =
    [
        "a", "al", "con", "de", "del", "e", "el", "en", "la", "las", "los", "para", "por", "sin", "un", "una", "unos", "unas",
        "bien", "chico", "chica", "chicos", "chicas", "comun", "cortado", "cortada", "cortados", "cortadas", "extra",
        "fresco", "fresca", "frescos", "frescas", "grande", "grandes", "mediano", "mediana", "medianos", "medianas",
        "molido", "molida", "molidos", "molidas", "opcional", "picado", "picada", "picados", "picadas", "rallado", "rallada"
    ];

    private readonly NidoDbContext _db;
    private readonly IResenaRecetaRepository _resenaRepository;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public RecetaRepository(NidoDbContext db, IResenaRecetaRepository resenaRepository, IPublicAssetUrlResolver assetUrlResolver)
    {
        _db = db;
        _resenaRepository = resenaRepository;
        _assetUrlResolver = assetUrlResolver;
    }

    public async Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var recetas = await _db.Recetas
            .AsNoTracking()
            .Include(receta => receta.IngredientesReceta)
                .ThenInclude(ingrediente => ingrediente.Producto)
            .Include(receta => receta.InfoNutricionalReceta)
            .Include(receta => receta.PasosReceta)
            .Include(receta => receta.RecetaElectrodomesticos)
            .OrderBy(receta => receta.Nombre)
            .ToListAsync(ct);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var diasAlerta = await GetDiasAlertaAsync(usuarioId, ct);
        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var productosPorVencer = await GetProductosPorVencerAsync(hogarId, hoy, diasAlerta, ct);
        var productosCompraEstandar = await GetProductosCompraEstandarAsync(ct);
        var vecesCocinadas = await GetVecesCocinadadasAsync(hogarId, ct);
        var resumenes = await _resenaRepository.GetResumenesAsync(recetas.Select(r => r.Id), hogarId, ct);
        var guardadas = await GetRecetasGuardadasIdsAsync(hogarId, ct);

        return recetas.Select(receta =>
            ToResult(
                receta,
                productosEnStock,
                productosPorVencer,
                productosCompraEstandar,
                hoy,
                vecesCocinadas.GetValueOrDefault(receta.Id, 0),
                resumenes.GetValueOrDefault(receta.Id, new ResenaResumen(0m, 0)),
                guardadas.Contains(receta.Id))).ToList();
    }

    public async Task<IReadOnlyList<RecetaResult>> GetSavedAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var savedIds = await GetRecetasGuardadasIdsAsync(hogarId, ct);
        if (savedIds.Count == 0)
        {
            return [];
        }

        var all = await GetAllAsync(hogarId, usuarioId, ct);
        return all.Where(receta => savedIds.Contains(receta.Id)).ToList();
    }

    public async Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Include(receta => receta.IngredientesReceta)
                .ThenInclude(ingrediente => ingrediente.Producto)
            .Include(receta => receta.InfoNutricionalReceta)
            .Include(receta => receta.PasosReceta)
            .Include(receta => receta.RecetaElectrodomesticos)
            .FirstOrDefaultAsync(ct);

        if (receta is null)
            return null;

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var diasAlerta = await GetDiasAlertaAsync(usuarioId, ct);
        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var productosPorVencer = await GetProductosPorVencerAsync(hogarId, hoy, diasAlerta, ct);
        var productosCompraEstandar = await GetProductosCompraEstandarAsync(ct);
        var vecesCocinada = await _db.RecetasCocinadas
            .AsNoTracking()
            .CountAsync(rc => rc.RecetaId == id && rc.HogarId == hogarId, ct);
        var resumen = await _resenaRepository.GetResumenAsync(id, hogarId, ct);
        var guardada = await _db.RecetasGuardadasHogar
            .AsNoTracking()
            .AnyAsync(saved => saved.HogarId == hogarId && saved.RecetaId == id, ct);

        return ToResult(receta, productosEnStock, productosPorVencer, productosCompraEstandar, hoy, vecesCocinada, resumen, guardada);
    }

    public async Task<bool> SaveAsync(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var recetaExists = await _db.Recetas.AnyAsync(receta => receta.Id == recetaId, ct);
        if (!recetaExists)
        {
            return false;
        }

        var alreadySaved = await _db.RecetasGuardadasHogar
            .AnyAsync(saved => saved.HogarId == hogarId && saved.RecetaId == recetaId, ct);

        if (!alreadySaved)
        {
            _db.RecetasGuardadasHogar.Add(new RecetaGuardadaHogar
            {
                HogarId = hogarId,
                RecetaId = recetaId,
                GuardadaPor = usuarioId,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> UnsaveAsync(Guid recetaId, Guid hogarId, CancellationToken ct)
    {
        var saved = await _db.RecetasGuardadasHogar
            .FirstOrDefaultAsync(saved => saved.HogarId == hogarId && saved.RecetaId == recetaId, ct);

        if (saved is null)
        {
            return false;
        }

        _db.RecetasGuardadasHogar.Remove(saved);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.Id == command.RecetaId)
            .Include(r => r.IngredientesReceta)
                .ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(ct);

        if (receta is null)
            return null;

        foreach (var ingrediente in receta.IngredientesReceta)
        {
            var consumo = RecipeUnitConverter.GetIngredientConsumption(ingrediente.Cantidad, ingrediente.Unidad);
            if (!consumo.HasValue)
                continue;

            await ReducirStockAsync(
                command.HogarId,
                ingrediente.ProductoId,
                BuildIngredientLookupName(ingrediente),
                consumo.Value.Cantidad,
                consumo.Value.Unidad,
                command.UsuarioId,
                ct);
        }

        var registro = new RecetasCocinada
        {
            Id = Guid.NewGuid(),
            RecetaId = command.RecetaId,
            HogarId = command.HogarId,
            CocinadoPor = command.UsuarioId,
            Fecha = DateTime.UtcNow,
            PorcionesCocinadas = receta.Porciones
        };

        _db.RecetasCocinadas.Add(registro);
        await _db.SaveChangesAsync(ct);

        var vecesCocinada = await _db.RecetasCocinadas
            .CountAsync(rc => rc.RecetaId == command.RecetaId && rc.HogarId == command.HogarId, ct);

        return new CocinarRecetaResult(command.RecetaId, vecesCocinada);
    }

    private async Task ReducirStockAsync(
        Guid hogarId,
        Guid? productoId,
        string? productoNombre,
        decimal cantidad,
        string? unidadIngrediente,
        Guid usuarioId,
        CancellationToken ct)
    {
        var stockItems = (await _db.StockHogars
            .Include(s => s.Producto)
            .Where(s => s.HogarId == hogarId
                     && (s.CantidadActual == null || s.CantidadActual > 0))
            .ToListAsync(ct))
            .Select(item => new
            {
                Item = item,
                Score = GetStockMatchScore(item, productoId, productoNombre)
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Item.FechaVencimiento ?? DateOnly.MaxValue)
            .ThenBy(match => match.Item.CreatedAt)
            .Select(match => match.Item)
            .ToList();

        var restante = cantidad;
        foreach (var item in stockItems)
        {
            if (restante <= 0) break;

            if (!item.CantidadActual.HasValue)
                continue;

            var disponible = item.CantidadActual.Value;
            var cantidadEnUnidadStock = RecipeUnitConverter.ConvertQuantity(restante, unidadIngrediente, item.UnidadMedida, productoNombre);

            if (!cantidadEnUnidadStock.HasValue)
                continue;

            // Siempre marcar como abierto al consumir (estuviera cerrado o no)
            item.EstaAbierto = true;
            item.UpdatedBy   = usuarioId;
            item.UpdatedAt   = DateTime.UtcNow;

            if (disponible <= cantidadEnUnidadStock.Value)
            {
                // Envase agotado: queda con cantidad=0 y abierto para descarte manual.
                restante -= RecipeUnitConverter.ConvertQuantity(disponible, item.UnidadMedida, unidadIngrediente, productoNombre) ?? 0;
                item.CantidadActual      = 0;
                item.PorcentajeConsumido = 100m;
            }
            else
            {
                // Reducción parcial:
                //  - bajamos CantidadActual por el consumo
                //  - recalculamos porcentajeConsumido respecto a la cantidad
                //    original del envase (inferida desde el estado previo)
                var nuevaCantidad = disponible - cantidadEnUnidadStock.Value;
                var cantidadOriginal = item.PorcentajeConsumido < 100m
                    ? disponible / ((100m - item.PorcentajeConsumido) / 100m)
                    : disponible;

                var nuevoPctConsumido = cantidadOriginal > 0m
                    ? Math.Clamp(((cantidadOriginal - nuevaCantidad) / cantidadOriginal) * 100m, 0m, 99m)
                    : item.PorcentajeConsumido;

                item.CantidadActual      = nuevaCantidad;
                item.PorcentajeConsumido = decimal.Round(nuevoPctConsumido, 2);
                restante = 0;
            }
        }
    }

    private static string BuildIngredientLookupName(IngredientesRecetum ingrediente)
        => $"{ingrediente.NombreIngrediente} {ingrediente.Producto?.Nombre}".Trim();

    private static int GetStockMatchScore(Nido.Infrastructure.Persistence.Entities.StockHogar stock, Guid? productoId, string? ingredientName)
    {
        if (productoId.HasValue && stock.ProductoId == productoId.Value)
            return 1000;

        return GetNameMatchScore(ingredientName, stock.Producto?.Nombre);
    }

    private static int GetNameMatchScore(string? ingredientName, string? productName)
    {
        var ingredient = NormalizeForMatch(ingredientName);
        var product = NormalizeForMatch(productName);

        if (string.IsNullOrWhiteSpace(ingredient) || string.IsNullOrWhiteSpace(product))
            return 0;

        if (ingredient == product)
            return 900;

        if (ContainsPhrase(ingredient, product))
            return 800;

        var ingredientTokens = GetMeaningfulTokens(ingredient);
        var productTokens = GetMeaningfulTokens(product);
        if (ingredientTokens.Count == 0 || productTokens.Count == 0)
            return 0;

        var matchingTokens = productTokens.Count(ingredientTokens.Contains);
        if (matchingTokens == productTokens.Count)
            return 700 + matchingTokens;

        return matchingTokens > 0 ? 500 + matchingTokens : 0;
    }

    private static bool ContainsPhrase(string text, string phrase)
        => $" {text} ".Contains($" {phrase} ", StringComparison.Ordinal);

    private static HashSet<string> GetMeaningfulTokens(string value)
        => value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeTokenForMatch)
            .Where(token => token.Length > 1 && !MatchStopWords.Contains(token))
            .ToHashSet();

    private static string NormalizeTokenForMatch(string token)
    {
        if (token.Length > 4 && token.EndsWith('s'))
            return token[..^1];

        return token;
    }

    private RecetaResult ToResult(
        Receta receta,
        IReadOnlySet<Guid> productosEnStock,
        IReadOnlyList<Nido.Infrastructure.Persistence.Entities.StockHogar> productosPorVencer,
        IReadOnlyList<ProductoCompraEstandar> productosCompraEstandar,
        DateOnly hoy,
        int vecesCocinada,
        ResenaResumen resumen,
        bool guardada)
    {
        var nutricion = receta.InfoNutricionalReceta.FirstOrDefault();
        var urgencia = CalculateUrgencia(receta, productosPorVencer, hoy);

        return new RecetaResult(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            _assetUrlResolver.Resolve(receta.ImagenUrl),
            nutricion?.Calorias,
            nutricion?.Proteinas,
            nutricion?.Carbohidratos,
            nutricion?.Grasas,
            receta.IngredientesReceta
                .OrderBy(ingrediente => ingrediente.NombreIngrediente)
                .Select(ingrediente =>
                {
                    var compraEstandar = ResolvePurchaseStandard(ingrediente, productosCompraEstandar);
                    var listaCompras = RecipeUnitConverter.ToShoppingListQuantity(
                        ingrediente.Cantidad,
                        ingrediente.Unidad,
                        BuildIngredientLookupName(ingrediente));
                    return new RecetaIngredienteResult(
                        ingrediente.Id,
                        ingrediente.ProductoId,
                        ingrediente.NombreIngrediente,
                        ingrediente.Producto != null ? ingrediente.Producto.Nombre : null,
                        ingrediente.Cantidad,
                        ingrediente.Unidad,
                        compraEstandar?.Cantidad,
                        compraEstandar?.Unidad,
                        listaCompras?.Cantidad,
                        listaCompras?.Unidad,
                        ingrediente.ProductoId.HasValue && productosEnStock.Contains(ingrediente.ProductoId.Value),
                        DetectAlergenos(ingrediente));
                })
                .ToList(),
            receta.PasosReceta
                .OrderBy(paso => paso.Orden)
                .Select(paso => new RecetaPasoResult(
                    paso.Id,
                    paso.Orden,
                    paso.Descripcion))
                .ToList(),
            receta.RecetaElectrodomesticos
                .OrderBy(electrodomestico => electrodomestico.TipoRequerido)
                .Select(electrodomestico => new RecetaElectrodomesticoResult(
                    electrodomestico.Id,
                    electrodomestico.TipoRequerido))
                .ToList(),
            vecesCocinada,
            resumen.Promedio,
            resumen.Total,
            urgencia.TieneProductosPorVencer,
            urgencia.FechaVencimientoMasProxima?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            urgencia.DiasHastaVencimiento,
            urgencia.ProductosPorVencer.Select(producto => new RecetaProductoPorVencerResult(
                producto.ProductoId,
                producto.Nombre,
                producto.FechaVencimiento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                producto.FechaVencimiento.DayNumber - hoy.DayNumber)).ToList(),
            guardada);
    }

    private static UrgenciaReceta CalculateUrgencia(
        Receta receta,
        IReadOnlyList<Nido.Infrastructure.Persistence.Entities.StockHogar> productosPorVencer,
        DateOnly hoy)
    {
        var productosUrgentes = receta.IngredientesReceta
            .SelectMany(ingrediente =>
            {
                var ingredientName = BuildIngredientLookupName(ingrediente);
                return productosPorVencer
                    .Where(stock => GetStockMatchScore(stock, ingrediente.ProductoId, ingredientName) > 0)
                    .Select(stock => new ProductoPorVencerReceta(
                        stock.ProductoId,
                        stock.Producto?.Nombre ?? ingredientName,
                        stock.FechaVencimiento!.Value));
            })
            .Where(producto => !string.IsNullOrWhiteSpace(producto.Nombre))
            .GroupBy(producto => producto.ProductoId)
            .Select(grupo => grupo
                .OrderBy(producto => producto.FechaVencimiento)
                .ThenBy(producto => producto.Nombre)
                .First())
            .OrderBy(producto => producto.FechaVencimiento)
            .ThenBy(producto => producto.Nombre)
            .ToList();

        var fechaMasProxima = productosUrgentes
            .Select(producto => (DateOnly?)producto.FechaVencimiento)
            .FirstOrDefault();

        return fechaMasProxima.HasValue
            ? new UrgenciaReceta(true, fechaMasProxima.Value, fechaMasProxima.Value.DayNumber - hoy.DayNumber, productosUrgentes)
            : new UrgenciaReceta(false, null, null, []);
    }

    private static IReadOnlyList<string> DetectAlergenos(IngredientesRecetum ingrediente)
    {
        var text = Normalize($"{ingrediente.NombreIngrediente} {ingrediente.Producto?.Nombre}");
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return AllergenAliases
            .Where(entry => entry.Value.Any(alias => text.Contains(Normalize(alias), StringComparison.Ordinal)))
            .Select(entry => entry.Key)
            .ToList();
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string NormalizeForMatch(string? value)
    {
        var normalized = Normalize(value ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var builder = new StringBuilder(normalized.Length);
        var previousWasSpace = true;

        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private async Task<IReadOnlyList<ProductoCompraEstandar>> GetProductosCompraEstandarAsync(CancellationToken ct)
    {
        return await _db.Productos
            .AsNoTracking()
            .Where(producto =>
                producto.CantidadCompraEstandar.HasValue &&
                !string.IsNullOrWhiteSpace(producto.UnidadCompraEstandar))
            .Select(producto => new ProductoCompraEstandar(
                producto.Id,
                producto.Nombre,
                producto.CantidadCompraEstandar!.Value,
                producto.UnidadCompraEstandar!))
            .ToListAsync(ct);
    }

    private static ProductoCompraEstandar? ResolvePurchaseStandard(
        IngredientesRecetum ingrediente,
        IReadOnlyList<ProductoCompraEstandar> productosCompraEstandar)
    {
        if (ingrediente.Producto is not null &&
            ingrediente.Producto.CantidadCompraEstandar.HasValue &&
            !string.IsNullOrWhiteSpace(ingrediente.Producto.UnidadCompraEstandar))
        {
            return new ProductoCompraEstandar(
                ingrediente.Producto.Id,
                ingrediente.Producto.Nombre,
                ingrediente.Producto.CantidadCompraEstandar.Value,
                ingrediente.Producto.UnidadCompraEstandar!);
        }

        var ingredientName = BuildIngredientLookupName(ingrediente);
        return productosCompraEstandar
            .Select(producto => new
            {
                Producto = producto,
                Score = GetNameMatchScore(ingredientName, producto.Nombre)
            })
            .Where(match => match.Score >= 700)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Producto.Nombre.Length)
            .Select(match => (ProductoCompraEstandar?)match.Producto)
            .FirstOrDefault();
    }

    private static bool TryReadUnicodeFraction(char value, out decimal quantity)
    {
        quantity = value switch
        {
            '¼' => 0.25m,
            '½' => 0.5m,
            '¾' => 0.75m,
            '⅓' => 1m / 3m,
            '⅔' => 2m / 3m,
            '⅛' => 0.125m,
            '⅜' => 0.375m,
            '⅝' => 0.625m,
            '⅞' => 0.875m,
            _ => 0m
        };

        return quantity > 0;
    }

    private static bool IsQuantityChar(char value)
        => char.IsDigit(value) || value is '/' or '.' or ',';

    private static bool AreSameQuantity(decimal left, decimal right)
        => Math.Abs(left - right) < 0.0001m;

    private readonly record struct ProductoCompraEstandar(Guid Id, string Nombre, decimal Cantidad, string Unidad);

    private readonly record struct UrgenciaReceta(
        bool TieneProductosPorVencer,
        DateOnly? FechaVencimientoMasProxima,
        int? DiasHastaVencimiento,
        IReadOnlyList<ProductoPorVencerReceta> ProductosPorVencer);

    private readonly record struct ProductoPorVencerReceta(
        Guid ProductoId,
        string Nombre,
        DateOnly FechaVencimiento);

    private async Task<int> GetDiasAlertaAsync(Guid usuarioId, CancellationToken ct)
    {
        const int defaultDiasAlerta = 7;

        if (usuarioId == Guid.Empty)
            return defaultDiasAlerta;

        var diasAlerta = await _db.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId)
            .Select(usuario => (int?)usuario.AlertaVencimientoDias)
            .FirstOrDefaultAsync(ct);

        return diasAlerta.HasValue
            ? Math.Clamp(diasAlerta.Value, 1, 365)
            : defaultDiasAlerta;
    }

    private async Task<IReadOnlyList<Nido.Infrastructure.Persistence.Entities.StockHogar>> GetProductosPorVencerAsync(
        Guid hogarId,
        DateOnly hoy,
        int diasAlerta,
        CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return [];

        var hasta = hoy.AddDays(diasAlerta);

        return await _db.StockHogars
            .AsNoTracking()
            .Include(stock => stock.Producto)
            .Where(stock => stock.HogarId == hogarId
                         && (stock.CantidadActual == null || stock.CantidadActual > 0)
                         && stock.FechaVencimiento.HasValue
                         && stock.FechaVencimiento.Value >= hoy
                         && stock.FechaVencimiento.Value <= hasta)
            .ToListAsync(ct);
    }

    private async Task<IReadOnlySet<Guid>> GetProductosEnStockAsync(Guid hogarId, CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return new HashSet<Guid>();

        var productoIds = await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId && (stock.CantidadActual == null || stock.CantidadActual > 0))
            .Select(stock => stock.ProductoId)
            .Distinct()
            .ToListAsync(ct);

        return productoIds.ToHashSet();
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetVecesCocinadadasAsync(Guid hogarId, CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return new Dictionary<Guid, int>();

        return await _db.RecetasCocinadas
            .AsNoTracking()
            .Where(rc => rc.HogarId == hogarId)
            .GroupBy(rc => rc.RecetaId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    private async Task<IReadOnlySet<Guid>> GetRecetasGuardadasIdsAsync(Guid hogarId, CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return new HashSet<Guid>();

        var ids = await _db.RecetasGuardadasHogar
            .AsNoTracking()
            .Where(saved => saved.HogarId == hogarId)
            .Select(saved => saved.RecetaId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task<RecipeImageTarget?> GetImageTargetAsync(Guid recipeId, CancellationToken cancellationToken)
        => await _db.Recetas
            .AsNoTracking()
            .Where(x => x.Id == recipeId)
            .Select(x => new RecipeImageTarget(x.Id, x.ImagenUrl))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdateImageKeyAsync(Guid recipeId, string storageKey, CancellationToken cancellationToken)
    {
        var receta = await _db.Recetas.FirstOrDefaultAsync(x => x.Id == recipeId, cancellationToken)
            ?? throw new RecipeImageTargetNotFoundException();
        receta.ImagenUrl = storageKey;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
