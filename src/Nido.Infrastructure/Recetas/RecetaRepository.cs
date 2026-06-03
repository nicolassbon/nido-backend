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

    private readonly NidoDbContext _db;

    public RecetaRepository(NidoDbContext db)
    {
        _db = db;
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

        return recetas.Select(receta =>
            ToResult(receta, productosEnStock, vecesCocinadas.GetValueOrDefault(receta.Id, 0))).ToList();
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

        return ToResult(receta, productosEnStock, vecesCocinada);
    }

    public async Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.Id == command.RecetaId)
            .Include(r => r.IngredientesReceta)
            .FirstOrDefaultAsync(ct);

        if (receta is null)
            return null;

        var ingredientesConProducto = receta.IngredientesReceta
            .Where(i => i.ProductoId.HasValue)
            .ToList();

        foreach (var ingrediente in ingredientesConProducto)
        {
            var consumo = GetIngredientConsumption(ingrediente);
            if (!consumo.HasValue)
                continue;

            await ReducirStockAsync(
                command.HogarId,
                ingrediente.ProductoId!.Value,
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
        Guid productoId,
        decimal cantidad,
        string? unidadIngrediente,
        Guid usuarioId,
        CancellationToken ct)
    {
        var stockItems = await _db.StockHogars
            .Where(s => s.HogarId == hogarId
                     && s.ProductoId == productoId
                     && (s.CantidadActual == null || s.CantidadActual > 0))
            .OrderBy(s => s.FechaVencimiento ?? DateOnly.MaxValue)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var restante = cantidad;
        foreach (var item in stockItems)
        {
            if (restante <= 0) break;

            if (!item.CantidadActual.HasValue)
                continue;

            var disponible = item.CantidadActual.Value;
            var cantidadEnUnidadStock = ConvertQuantity(restante, unidadIngrediente, item.UnidadMedida);

            if (!cantidadEnUnidadStock.HasValue)
                continue;

            if (disponible <= cantidadEnUnidadStock.Value)
            {
                restante -= ConvertQuantity(disponible, item.UnidadMedida, unidadIngrediente) ?? 0;
                _db.StockHogars.Remove(item);
            }
            else
            {
                item.CantidadActual = disponible - cantidadEnUnidadStock.Value;
                item.UpdatedBy = usuarioId;
                item.UpdatedAt = DateTime.UtcNow;
                restante = 0;
            }
        }
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

    private static RecetaResult ToResult(Receta receta, IReadOnlySet<Guid> productosEnStock, int vecesCocinada)
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
            vecesCocinada);
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

    private static decimal? ConvertQuantity(decimal quantity, string? fromUnit, string? toUnit)
    {
        var from = NormalizeUnit(fromUnit);
        var to = NormalizeUnit(toUnit);

        if (from.Family != to.Family)
            return null;

        return quantity * from.Factor / to.Factor;
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
            "" or "unidad" or "unidades" or "unid" or "u" or "ud" => new UnitInfo("count", 1m),
            "g" or "gr" or "grs" or "gramo" or "gramos" => new UnitInfo("mass", 1m),
            "kg" or "kilo" or "kilos" or "kilogramo" or "kilogramos" => new UnitInfo("mass", 1000m),
            "ml" or "mililitro" or "mililitros" => new UnitInfo("volume", 1m),
            "l" or "lt" or "lts" or "litro" or "litros" => new UnitInfo("volume", 1000m),
            "cdta" or "cdtas" or "cdita" or "cditas" or "cucharadita" or "cucharaditas" => new UnitInfo("volume", 5m),
            "cda" or "cdas" or "cucharada" or "cucharadas" => new UnitInfo("volume", 15m),
            "taza" or "tazas" => new UnitInfo("volume", 240m),
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
