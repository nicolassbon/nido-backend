using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Recetas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Recetas;

public sealed class RecetaRepository : IRecetaRepository
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

    private const decimal GenericGramsPerCup = 100m;
    private const decimal MillilitersPerCup = 240m;
    private const decimal GenericGramsPerUnit = 100m;
    private const decimal GenericMillilitersPerUnit = 100m;

    private readonly NidoDbContext _db;
    private readonly IResenaRecetaRepository _resenaRepository;

    public RecetaRepository(NidoDbContext db, IResenaRecetaRepository resenaRepository)
    {
        _db = db;
        _resenaRepository = resenaRepository;
    }

    public async Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, CancellationToken ct)
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

        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var vecesCocinadas = await GetVecesCocinadadasAsync(hogarId, ct);
        var resumenes = await _resenaRepository.GetResumenesAsync(recetas.Select(r => r.Id), hogarId, ct);

        return recetas.Select(receta =>
            ToResult(
                receta,
                productosEnStock,
                vecesCocinadas.GetValueOrDefault(receta.Id, 0),
                resumenes.GetValueOrDefault(receta.Id, new ResenaResumen(0m, 0)))).ToList();
    }

    public async Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
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

        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var vecesCocinada = await _db.RecetasCocinadas
            .AsNoTracking()
            .CountAsync(rc => rc.RecetaId == id && rc.HogarId == hogarId, ct);
        var resumen = await _resenaRepository.GetResumenAsync(id, hogarId, ct);

        return ToResult(receta, productosEnStock, vecesCocinada, resumen);
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
            var consumo = GetIngredientConsumption(ingrediente);
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
            var cantidadEnUnidadStock = ConvertQuantity(restante, unidadIngrediente, item.UnidadMedida, productoNombre);

            if (!cantidadEnUnidadStock.HasValue)
                continue;

            if (disponible <= cantidadEnUnidadStock.Value)
            {
                restante -= ConvertQuantity(disponible, item.UnidadMedida, unidadIngrediente, productoNombre) ?? 0;
                _db.StockHogars.Remove(item);
            }
            else
            {
                // Reducción parcial:
                //  - bajamos CantidadActual por el consumo
                //  - recalculamos porcentajeConsumido respecto a la cantidad
                //    original del envase (inferida desde el estado previo)
                //  - marcamos el envase como abierto
                var nuevaCantidad = disponible - cantidadEnUnidadStock.Value;
                var cantidadOriginal = item.PorcentajeConsumido < 100m
                    ? disponible / ((100m - item.PorcentajeConsumido) / 100m)
                    : disponible;

                var nuevoPctConsumido = cantidadOriginal > 0m
                    ? Math.Clamp(((cantidadOriginal - nuevaCantidad) / cantidadOriginal) * 100m, 0m, 99m)
                    : item.PorcentajeConsumido;

                item.CantidadActual       = nuevaCantidad;
                item.EstaAbierto          = true;
                item.PorcentajeConsumido  = decimal.Round(nuevoPctConsumido, 2);
                item.UpdatedBy            = usuarioId;
                item.UpdatedAt            = DateTime.UtcNow;
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

    private static IngredientConsumption? GetIngredientConsumption(IngredientesRecetum ingrediente)
    {
        if (ingrediente.Cantidad.HasValue && ingrediente.Cantidad.Value > 0)
        {
            if (TryReadLeadingQuantity(ingrediente.Unidad, out var embeddedQuantity, out var unit)
                && AreSameQuantity(ingrediente.Cantidad.Value, embeddedQuantity))
            {
                return new IngredientConsumption(ingrediente.Cantidad.Value, unit);
            }

            return new IngredientConsumption(ingrediente.Cantidad.Value, ingrediente.Unidad);
        }

        return TryReadLeadingQuantity(ingrediente.Unidad, out var quantityFromUnit, out var unitFromUnit)
            ? new IngredientConsumption(quantityFromUnit, unitFromUnit)
            : null;
    }

    private static RecetaResult ToResult(Receta receta, IReadOnlySet<Guid> productosEnStock, int vecesCocinada, ResenaResumen resumen)
    {
        var nutricion = receta.InfoNutricionalReceta.FirstOrDefault();

        return new RecetaResult(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            receta.ImagenUrl,
            nutricion?.Calorias,
            nutricion?.Proteinas,
            nutricion?.Carbohidratos,
            nutricion?.Grasas,
            receta.IngredientesReceta
                .OrderBy(ingrediente => ingrediente.NombreIngrediente)
                .Select(ingrediente => new RecetaIngredienteResult(
                    ingrediente.Id,
                    ingrediente.ProductoId,
                    ingrediente.NombreIngrediente,
                    ingrediente.Producto != null ? ingrediente.Producto.Nombre : null,
                    ingrediente.Cantidad,
                    ingrediente.Unidad,
                    ingrediente.ProductoId.HasValue && productosEnStock.Contains(ingrediente.ProductoId.Value),
                    DetectAlergenos(ingrediente)))
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
            resumen.Total);
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

    private static decimal? ConvertQuantity(decimal quantity, string? fromUnit, string? toUnit, string? ingredientName = null)
    {
        var from = NormalizeUnit(fromUnit);
        var to = NormalizeUnit(toUnit);

        if (from.Family == to.Family)
            return quantity * from.Factor / to.Factor;

        if (from.Family == "volume" && to.Family == "mass")
        {
            var volumeToMassDensity = GetDensityGramsPerMlOrDefault(ingredientName);
            var milliliters = quantity * from.Factor;
            var grams = milliliters * volumeToMassDensity;
            return grams / to.Factor;
        }

        if (from.Family == "mass" && to.Family == "volume")
        {
            var massToVolumeDensity = GetDensityGramsPerMlOrDefault(ingredientName);
            var grams = quantity * from.Factor;
            var milliliters = grams / massToVolumeDensity;
            return milliliters / to.Factor;
        }

        if (from.Family == "count" && to.Family == "mass")
            return quantity * GenericGramsPerUnit / to.Factor;

        if (from.Family == "count" && to.Family == "volume")
            return quantity * GenericMillilitersPerUnit / to.Factor;

        if (from.Family == "mass" && to.Family == "count")
            return quantity * from.Factor / GenericGramsPerUnit;

        if (from.Family == "volume" && to.Family == "count")
            return quantity * from.Factor / GenericMillilitersPerUnit;

        return null;
    }

    private static decimal GetDensityGramsPerMlOrDefault(string? ingredientName)
        => TryGetDensityGramsPerMl(ingredientName, out var gramsPerMl)
            ? gramsPerMl
            : GenericGramsPerCup / MillilitersPerCup;

    private static bool TryGetDensityGramsPerMl(string? ingredientName, out decimal gramsPerMl)
    {
        var normalized = Normalize(ingredientName ?? string.Empty);

        gramsPerMl = normalized switch
        {
            var name when name.Contains("harina", StringComparison.Ordinal) => 120m / 240m,
            var name when name.Contains("arroz", StringComparison.Ordinal) => 198m / 240m,
            var name when name.Contains("arveja", StringComparison.Ordinal) => 160m / 240m,
            var name when name.Contains("pasa", StringComparison.Ordinal) => 149m / 240m,
            var name when name.Contains("cebolla", StringComparison.Ordinal) => 142m / 240m,
            var name when name.Contains("queso", StringComparison.Ordinal) => 113m / 240m,
            var name when name.Contains("manteca", StringComparison.Ordinal)
                || name.Contains("mantequilla", StringComparison.Ordinal) => 226m / 240m,
            var name when name.Contains("aceite", StringComparison.Ordinal) => 200m / 240m,
            var name when name.Contains("leche", StringComparison.Ordinal) => 227m / 240m,
            var name when name.Contains("agua", StringComparison.Ordinal)
                || name.Contains("caldo", StringComparison.Ordinal)
                || name.Contains("jugo", StringComparison.Ordinal)
                || name.Contains("salsa", StringComparison.Ordinal) => 1m,
            var name when ContainsWord(name, "sal") => 18m / 15m,
            var name when name.Contains("azucar", StringComparison.Ordinal) => 198m / 240m,
            var name when name.Contains("hongo", StringComparison.Ordinal)
                || name.Contains("champinon", StringComparison.Ordinal)
                || name.Contains("champignon", StringComparison.Ordinal) => 78m / 240m,
            var name when name.Contains("zanahoria", StringComparison.Ordinal) => 142m / 240m,
            var name when name.Contains("apio", StringComparison.Ordinal) => 142m / 240m,
            _ => 0m
        };

        return gramsPerMl > 0;
    }

    private static bool ContainsWord(string value, string word)
    {
        var padded = $" {value} ";
        return padded.Contains($" {word} ", StringComparison.Ordinal)
            || padded.Contains($" {word}.", StringComparison.Ordinal)
            || padded.Contains($" {word},", StringComparison.Ordinal);
    }

    private static UnitInfo NormalizeUnit(string? unit)
    {
        var multiplier = 1m;
        var unitValue = unit;
        if (TryReadLeadingQuantity(unit, out var quantity, out var unitWithoutQuantity))
        {
            multiplier = quantity;
            unitValue = unitWithoutQuantity;
        }

        var normalized = Normalize(unitValue ?? string.Empty)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var normalizedUnit = normalized switch
        {
            "" or "unidad" or "unidades" or "unid" or "u" or "ud"
                or "diente" or "dientes" or "hoja" or "hojas" or "lata" or "latas"
                or "paquete" or "paquetes" or "pieza" or "piezas" or "pote" or "potes"
                or "frasco" or "frascos" or "rodaja" or "rodajas" or "sobre" or "sobres"
                or "tallo" or "tallos" => new UnitInfo("count", 1m),
            "mg" or "miligramo" or "miligramos" => new UnitInfo("mass", 0.001m),
            "g" or "gr" or "grs" or "gramo" or "gramos" => new UnitInfo("mass", 1m),
            "kg" or "kilo" or "kilos" or "kilogramo" or "kilogramos" => new UnitInfo("mass", 1000m),
            "oz" or "onza" or "onzas" => new UnitInfo("mass", 28.3495m),
            "lb" or "lbs" or "libra" or "libras" => new UnitInfo("mass", 453.592m),
            "pizca" or "pizcas" or "pinch" => new UnitInfo("mass", 0.5m),
            "ml" or "mililitro" or "mililitros" or "cc" or "cm3" => new UnitInfo("volume", 1m),
            "cl" or "centilitro" or "centilitros" => new UnitInfo("volume", 10m),
            "dl" or "decilitro" or "decilitros" => new UnitInfo("volume", 100m),
            "l" or "lt" or "lts" or "litro" or "litros" => new UnitInfo("volume", 1000m),
            "cdta" or "cdtas" or "cdita" or "cditas" or "cucharadita" or "cucharaditas"
                or "tsp" or "teaspoon" or "teaspoons" => new UnitInfo("volume", 5m),
            "cda" or "cdas" or "cucharada" or "cucharadas"
                or "tbsp" or "tablespoon" or "tablespoons" => new UnitInfo("volume", 15m),
            "chorrito" or "chorritos" or "splash" => new UnitInfo("volume", 15m),
            "taza" or "tazas" or "cup" or "cups" => new UnitInfo("volume", MillilitersPerCup),
            _ => new UnitInfo($"custom:{normalized}", 1m)
        };

        return normalizedUnit with { Factor = normalizedUnit.Factor * multiplier };
    }

    private static bool TryReadLeadingQuantity(string? value, out decimal quantity, out string unit)
    {
        quantity = 0;
        unit = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();
        if (TryReadUnicodeFraction(text[0], out var unicodeFraction))
        {
            var remaining = text[1..].Trim();
            if (string.IsNullOrWhiteSpace(remaining))
                return false;

            quantity = unicodeFraction;
            unit = remaining;
            return true;
        }

        var firstTokenLength = 0;
        while (firstTokenLength < text.Length && IsQuantityChar(text[firstTokenLength]))
        {
            firstTokenLength++;
        }

        if (firstTokenLength == 0)
            return false;

        var firstToken = text[..firstTokenLength];
        if (!TryParseQuantityToken(firstToken, out var parsedQuantity))
            return false;

        var rest = text[firstTokenLength..].TrimStart();
        if (TryConsumeFractionToken(rest, out var fraction, out var restAfterFraction))
        {
            parsedQuantity += fraction;
            rest = restAfterFraction.TrimStart();
        }

        if (string.IsNullOrWhiteSpace(rest))
            return false;

        quantity = parsedQuantity;
        unit = rest;
        return true;
    }

    private static bool TryConsumeFractionToken(string value, out decimal fraction, out string rest)
    {
        fraction = 0;
        rest = value;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.TrimStart();
        if (TryReadUnicodeFraction(text[0], out fraction))
        {
            rest = text[1..];
            return true;
        }

        var tokenLength = 0;
        while (tokenLength < text.Length && IsQuantityChar(text[tokenLength]))
        {
            tokenLength++;
        }

        if (tokenLength == 0)
            return false;

        var token = text[..tokenLength];
        if (!token.Contains('/', StringComparison.Ordinal) || !TryParseQuantityToken(token, out fraction))
            return false;

        rest = text[tokenLength..];
        return true;
    }

    private static bool TryParseQuantityToken(string token, out decimal quantity)
    {
        quantity = 0;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var normalized = token.Trim().Replace(',', '.');
        if (normalized.Contains('/', StringComparison.Ordinal))
        {
            var parts = normalized.Split('/', 2);
            if (parts.Length != 2
                || !decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var numerator)
                || !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var denominator)
                || denominator == 0)
            {
                return false;
            }

            quantity = numerator / denominator;
            return quantity > 0;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity)
            && quantity > 0;
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

    private readonly record struct IngredientConsumption(decimal Cantidad, string? Unidad);

    private readonly record struct UnitInfo(string Family, decimal Factor);

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
}
