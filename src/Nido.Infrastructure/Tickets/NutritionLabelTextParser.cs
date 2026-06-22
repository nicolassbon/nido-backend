using Nido.Application.Alacena;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nido.Infrastructure.Tickets;

internal static class NutritionLabelTextParser
{
    private static readonly Regex ValueRegex = new(
        @"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>kcal|kj|mg|gr|g|ml|mcg|µg)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PercentRegex = new(
        @"(?<value>\d+(?:[.,]\d+)?)\s*%",
        RegexOptions.Compiled);

    public static NutritionInfoResult Parse(string? text)
    {
        var lines = (text ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => Clean(line))
            .Where(line => line.Length > 0)
            .ToArray();

        var items = new List<NutritionInfoItemResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            AddItem(items, seen, ParseLine(line, items.Count + 1));
        }

        foreach (var item in ParseSplitTableLines(lines, items.Count + 1))
        {
            AddItem(items, seen, item);
        }

        decimal? calorias = FindValue(items, "Valor energetico");
        decimal? proteinas = FindValue(items, "Proteinas");
        decimal? carbohidratos = FindValue(items, "Carbohidratos");
        decimal? grasas = FindValue(items, "Grasas");

        return new NutritionInfoResult(
            calorias,
            proteinas,
            carbohidratos,
            grasas,
            ExtractServing(lines),
            ExtractBase(lines),
            items);
    }

    private static NutritionInfoItemResult? ParseLine(string line, int order)
    {
        var matches = ValueRegex.Matches(line);
        if (matches.Count == 0)
        {
            return null;
        }

        var firstValueIndex = matches[0].Index;
        var rawName = Clean(line[..firstValueIndex]);
        var name = DisplayName(rawName);
        if (name is null)
        {
            return null;
        }

        var preferred = ChoosePreferredValue(name, matches);
        if (preferred is null)
        {
            return null;
        }

        var percent = PercentRegex.Matches(line)
            .Select(match => ParseDecimal(match.Groups["value"].Value))
            .LastOrDefault(value => value.HasValue);

        return new NutritionInfoItemResult(
            name,
            preferred.Value.Value,
            NormalizeUnit(preferred.Value.Unit),
            percent,
            order);
    }

    private static IReadOnlyList<NutritionInfoItemResult> ParseSplitTableLines(IReadOnlyList<string> lines, int firstOrder)
    {
        var labels = lines
            .Where(line => ValueRegex.Matches(line).Count == 0)
            .Select(DisplayName)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        var values = lines
            .Select(line => new { Line = line, Matches = ValueRegex.Matches(line) })
            .Where(item => item.Matches.Count > 0 && !Clean(item.Line[..item.Matches[0].Index]).Any(char.IsLetter))
            .ToArray();

        var count = Math.Min(labels.Length, values.Length);
        var items = new List<NutritionInfoItemResult>(count);

        for (var index = 0; index < count; index++)
        {
            var preferred = ChoosePreferredValue(labels[index], values[index].Matches);
            if (preferred is null)
            {
                continue;
            }

            var percent = PercentRegex.Matches(values[index].Line)
                .Select(match => ParseDecimal(match.Groups["value"].Value))
                .LastOrDefault(value => value.HasValue);

            items.Add(new NutritionInfoItemResult(
                labels[index],
                preferred.Value.Value,
                NormalizeUnit(preferred.Value.Unit),
                percent,
                firstOrder + items.Count));
        }

        return items;
    }

    private static void AddItem(
        List<NutritionInfoItemResult> items,
        HashSet<string> seen,
        NutritionInfoItemResult? item)
    {
        if (item is null)
        {
            return;
        }

        var key = Normalize(item.Nombre);
        if (!seen.Add(key))
        {
            return;
        }

        items.Add(item);
    }

    private static (decimal? Value, string Unit)? ChoosePreferredValue(string name, MatchCollection matches)
    {
        var values = matches
            .Select(match => new
            {
                Value = ParseDecimal(match.Groups["value"].Value),
                Unit = match.Groups["unit"].Value.ToLowerInvariant()
            })
            .Where(item => item.Value.HasValue)
            .ToArray();

        if (values.Length == 0)
        {
            return null;
        }

        if (name.Equals("Valor energetico", StringComparison.OrdinalIgnoreCase))
        {
            var kcal = values.LastOrDefault(item => item.Unit.Equals("kcal", StringComparison.OrdinalIgnoreCase));
            if (kcal is not null)
            {
                return (kcal.Value, kcal.Unit);
            }
        }

        var grams = values.LastOrDefault(item =>
            item.Unit is "g" or "gr" or "mg" or "mcg" or "ug" or "µg" or "ml");

        var selected = grams ?? values.Last();
        return (selected.Value, selected.Unit);
    }

    private static decimal? FindValue(IEnumerable<NutritionInfoItemResult> items, string name)
        => items.FirstOrDefault(item => item.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase))?.Valor;

    private static string? DisplayName(string rawName)
    {
        var canonical = CanonicalName(rawName);
        if (canonical is not null)
        {
            return canonical;
        }

        var cleaned = CleanLabel(rawName);
        if (cleaned is null || IsIgnoredLabel(cleaned))
        {
            return null;
        }

        return cleaned;
    }

    private static string? CanonicalName(string rawName)
    {
        var normalized = Normalize(rawName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Contains("valor energetico") || normalized is "energia" or "calorias")
            return "Valor energetico";
        if (normalized.Contains("saturad"))
            return "Grasas saturadas";
        if (normalized.Contains("trans"))
            return "Grasas trans";
        if (normalized.Contains("grasa") || normalized.Contains("lipido"))
            return "Grasas";
        if (normalized.Contains("hidratos") || normalized.Contains("carbohidratos") || normalized.Contains("carbono"))
            return "Carbohidratos";
        if (normalized.Contains("azucar"))
            return "Azucares";
        if (normalized.Contains("fibra"))
            return "Fibra alimentaria";
        if (normalized.Contains("proteina"))
            return "Proteinas";
        if (normalized.Contains("sodio"))
            return "Sodio";
        if (normalized.Contains("calcio"))
            return "Calcio";
        if (normalized.Contains("vitamina a"))
            return "Vitamina A";
        if (normalized.Contains("vitamina d"))
            return "Vitamina D";
        if (normalized.Contains("vitamina"))
            return CleanLabel(rawName);
        if (normalized.Equals("sal") || normalized.Contains(" sal"))
            return "Sal";

        return null;
    }

    private static string? CleanLabel(string value)
    {
        var cleaned = Clean(Regex.Replace(value, @"[*():_%\d.,/=-]+", " "));
        if (cleaned.Length < 2 || !cleaned.Any(char.IsLetter))
        {
            return null;
        }

        return TrimToMax(cleaned, 150);
    }

    private static bool IsIgnoredLabel(string value)
    {
        var normalized = Normalize(value);
        return normalized.Contains("informacion nutricional") ||
            normalized.Contains("porcion") ||
            normalized.Contains("racion") ||
            normalized.Contains("por 100") ||
            normalized is "vd" ||
            normalized.Contains("valores diarios") ||
            normalized.Contains("dieta") ||
            normalized.Contains("sus valores") ||
            normalized.Contains("medallon");
    }

    private static string? ExtractServing(IEnumerable<string> lines)
    {
        var servingLine = lines.FirstOrDefault(line =>
            Normalize(line).Contains("racion") ||
            Normalize(line).Contains("porcion"));

        return servingLine is null ? null : TrimToMax(servingLine, 100);
    }

    private static string? ExtractBase(IEnumerable<string> lines)
    {
        var joined = Normalize(string.Join(' ', lines));
        if (joined.Contains("por racion") || joined.Contains("por porcion"))
        {
            return "Por porcion";
        }

        if (joined.Contains("por 100"))
        {
            return "Por 100";
        }

        return null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
    }

    private static string Clean(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string Normalize(string value)
    {
        var normalized = value
            .Normalize(NormalizationForm.FormD)
            .ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("de los cuales", string.Empty)
            .Replace("de las cuales", string.Empty)
            .Trim();
    }

    private static string NormalizeUnit(string value)
        => value.Equals("gr", StringComparison.OrdinalIgnoreCase)
            ? "g"
            : value.Equals("kj", StringComparison.OrdinalIgnoreCase)
            ? "kJ"
            : value.ToLowerInvariant();

    private static string TrimToMax(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
