namespace Nido.Infrastructure.Productos;

/// <summary>
/// Mapea categorías crudas devueltas por las APIs externas (Open Food Facts,
/// UPC Item DB) a las categorías canónicas de Nido: General, Lácteos, Bebidas,
/// Congelados, Despensa.
///
/// El matching es sobre keywords contenidos en los tags. Se devuelve la primera
/// coincidencia (los tags vienen ordenados de más específico a más genérico).
/// Si no hay match → "General".
/// </summary>
public sealed class ProductCategoryMapper
{
    public const string General    = "General";
    public const string Lacteos    = "Lácteos";
    public const string Bebidas    = "Bebidas";
    public const string Congelados = "Congelados";
    public const string Despensa   = "Despensa";

    // Lista de (keyword → categoría) ordenada por especificidad. Más arriba =
    // más específico. Importa el orden: "ice-cream" antes que "frozen" porque
    // un helado tiene ambos tags y queremos "Congelados".
    private static readonly (string Keyword, string Categoria)[] Mappings = new[]
    {
        // Congelados (antes que lácteos: el helado tiene tags de dairy también)
        ("ice-cream",         Congelados),
        ("ice-creams",        Congelados),
        ("frozen",            Congelados),

        // Lácteos
        ("dairies",           Lacteos),
        ("dairy",             Lacteos),
        ("milks",             Lacteos),
        ("milk",              Lacteos),
        ("cheeses",           Lacteos),
        ("cheese",            Lacteos),
        ("yogurts",           Lacteos),
        ("yogurt",            Lacteos),
        ("yoghurts",          Lacteos),
        ("yoghurt",           Lacteos),
        ("butters",           Lacteos),
        ("butter",            Lacteos),
        ("creams",            Lacteos),
        ("dulce-de-leche",    Lacteos),

        // Bebidas
        ("beverages",         Bebidas),
        ("drinks",            Bebidas),
        ("juices",            Bebidas),
        ("juice",             Bebidas),
        ("waters",            Bebidas),
        ("water",             Bebidas),
        ("sodas",             Bebidas),
        ("soft-drinks",       Bebidas),
        ("coffees",           Bebidas),
        ("coffee",            Bebidas),
        ("teas",              Bebidas),
        ("wines",             Bebidas),
        ("beers",             Bebidas),

        // Despensa
        ("cereals",           Despensa),
        ("pastas",            Despensa),
        ("pasta",             Despensa),
        ("rices",             Despensa),
        ("rice",              Despensa),
        ("oils",              Despensa),
        ("vinegars",          Despensa),
        ("sauces",            Despensa),
        ("condiments",        Despensa),
        ("spices",            Despensa),
        ("salts",             Despensa),
        ("sugars",            Despensa),
        ("flours",            Despensa),
        ("breads",            Despensa),
        ("breakfasts",        Despensa),
        ("snacks",            Despensa),
        ("biscuits",          Despensa),
        ("cookies",           Despensa),
        ("chocolates",        Despensa),
        ("jams",              Despensa),
        ("honeys",            Despensa),
        ("canned-foods",      Despensa),
        ("legumes",           Despensa),
        ("nuts",              Despensa),
    };

    public string Map(IReadOnlyCollection<string>? tags)
    {
        if (tags is null || tags.Count == 0) return General;

        foreach (var rawTag in tags)
        {
            var normalized = NormalizeTag(rawTag);
            foreach (var (keyword, categoria) in Mappings)
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                    return categoria;
            }
        }

        return General;
    }

    /// <summary>"en:dairies-fr" → "dairies-fr"</summary>
    private static string NormalizeTag(string tag)
    {
        var trimmed = tag.Trim().ToLowerInvariant();
        var colon   = trimmed.IndexOf(':');
        return colon >= 0 ? trimmed[(colon + 1)..] : trimmed;
    }
}
