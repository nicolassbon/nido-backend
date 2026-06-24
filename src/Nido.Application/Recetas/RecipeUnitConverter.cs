using System.Globalization;
using System.Text;

namespace Nido.Application.Recetas;

public static class RecipeUnitConverter
{
    private const decimal GenericGramsPerCup = 100m;
    private const decimal MillilitersPerCup = 240m;
    private const decimal MillilitersPerGlass = 250m;
    private const decimal MillilitersPerPinch = 0.3m;
    private const decimal GenericGramsPerUnit = 100m;
    private const decimal GenericMillilitersPerUnit = 100m;

    public static IngredientQuantity? GetIngredientConsumption(decimal? cantidad, string? unidad)
    {
        if (cantidad.HasValue && cantidad.Value > 0)
        {
            if (TryReadLeadingQuantity(unidad, out var embeddedQuantity, out var unit)
                && AreSameQuantity(cantidad.Value, embeddedQuantity))
            {
                return new IngredientQuantity(cantidad.Value, unit);
            }

            return new IngredientQuantity(cantidad.Value, unidad);
        }

        return TryReadLeadingQuantity(unidad, out var quantityFromUnit, out var unitFromUnit)
            ? new IngredientQuantity(quantityFromUnit, unitFromUnit)
            : null;
    }

    public static decimal? ConvertQuantity(decimal quantity, string? fromUnit, string? toUnit, string? ingredientName = null)
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

    public static IngredientQuantity? ToShoppingListQuantity(decimal? cantidad, string? unidad, string? ingredientName = null)
    {
        var consumption = GetIngredientConsumption(cantidad, unidad);
        if (!consumption.HasValue)
            return null;

        var source = NormalizeUnit(consumption.Value.Unidad);
        if (source.Family == "mass")
        {
            var grams = consumption.Value.Cantidad * source.Factor;
            return NormalizeMass(grams);
        }

        if (source.Family == "volume")
        {
            var milliliters = consumption.Value.Cantidad * source.Factor;
            if (TryGetDensityGramsPerMl(ingredientName, out var density) && !IsLiquidIngredient(ingredientName))
            {
                return NormalizeMass(milliliters * density);
            }

            return NormalizeVolume(milliliters);
        }

        return consumption.Value;
    }

    private static IngredientQuantity NormalizeMass(decimal grams)
        => grams >= 1000m
            ? new IngredientQuantity(decimal.Round(grams / 1000m, 2), "kg")
            : new IngredientQuantity(decimal.Round(grams, 2), "g");

    private static IngredientQuantity NormalizeVolume(decimal milliliters)
        => milliliters >= 1000m
            ? new IngredientQuantity(decimal.Round(milliliters / 1000m, 2), "lt")
            : new IngredientQuantity(decimal.Round(milliliters, 2), "ml");

    private static bool IsLiquidIngredient(string? ingredientName)
    {
        var normalized = Normalize(ingredientName ?? string.Empty);

        return normalized.Contains("aceite", StringComparison.Ordinal)
            || normalized.Contains("agua", StringComparison.Ordinal)
            || normalized.Contains("caldo", StringComparison.Ordinal)
            || normalized.Contains("extracto", StringComparison.Ordinal)
            || normalized.Contains("jugo", StringComparison.Ordinal)
            || normalized.Contains("leche", StringComparison.Ordinal)
            || normalized.Contains("salsa", StringComparison.Ordinal)
            || normalized.Contains("vinagre", StringComparison.Ordinal)
            || normalized.Contains("vino", StringComparison.Ordinal);
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
            var name when name.Contains("pimenton", StringComparison.Ordinal)
                || name.Contains("chile", StringComparison.Ordinal)
                || name.Contains("oregano", StringComparison.Ordinal)
                || name.Contains("comino", StringComparison.Ordinal)
                || name.Contains("curcuma", StringComparison.Ordinal)
                || name.Contains("curry", StringComparison.Ordinal)
                || name.Contains("pimienta", StringComparison.Ordinal)
                || name.Contains("nuez moscada", StringComparison.Ordinal)
                || name.Contains("canela", StringComparison.Ordinal)
                || name.Contains("mostaza", StringComparison.Ordinal)
                || name.Contains("polvo", StringComparison.Ordinal) => 2.5m / 5m,
            _ => 0m
        };

        return gramsPerMl > 0;
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
            "pizca" or "pizcas" or "pinch" => new UnitInfo("volume", MillilitersPerPinch),
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
            "vaso" or "vasos" or "glass" or "glasses" => new UnitInfo("volume", MillilitersPerGlass),
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
            '\u00BC' => 0.25m,
            '\u00BD' => 0.5m,
            '\u00BE' => 0.75m,
            '\u2153' => 1m / 3m,
            '\u2154' => 2m / 3m,
            '\u215B' => 0.125m,
            '\u215C' => 0.375m,
            '\u215D' => 0.625m,
            '\u215E' => 0.875m,
            _ => 0m
        };

        return quantity > 0;
    }

    private static bool IsQuantityChar(char value)
        => char.IsDigit(value) || value is '/' or '.' or ',';

    private static bool AreSameQuantity(decimal left, decimal right)
        => Math.Abs(left - right) < 0.0001m;

    private static bool ContainsWord(string value, string word)
    {
        var padded = $" {value} ";
        return padded.Contains($" {word} ", StringComparison.Ordinal)
            || padded.Contains($" {word}.", StringComparison.Ordinal)
            || padded.Contains($" {word},", StringComparison.Ordinal);
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

    private readonly record struct UnitInfo(string Family, decimal Factor);
}

public readonly record struct IngredientQuantity(decimal Cantidad, string? Unidad);
