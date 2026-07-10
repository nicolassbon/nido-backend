using System.Globalization;
using System.Text;

namespace Nido.Application.Common;

/// <summary>
/// Resuelve un ícono Lucide para un producto: primero por categoría (si está mapeada),
/// si no por palabras clave en el nombre. Fuente única para que lista de compras y
/// sugerencias de Nido muestren siempre el mismo ícono para un mismo producto.
/// </summary>
public static class ProductCategoryIconResolver
{
    private static readonly Dictionary<string, string> CategoryIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Aceites"] = "droplet",
        ["Arroz"] = "wheat",
        ["Azúcar y Endulzantes"] = "sugar",
        ["Bebés"] = "baby",
        ["Bebidas"] = "glass-water",
        ["Bebidas Alcohólicas"] = "wine",
        ["Carnes Porcinas"] = "beef",
        ["Carnes Vacunas"] = "beef",
        ["Cereales"] = "wheat",
        ["Condimentos"] = "leaf",
        ["Congelados"] = "snowflake",
        ["Conservas"] = "archive",
        ["Farmacia"] = "pill",
        ["Fiambres y Embutidos"] = "sausage",
        ["Frutas"] = "apple",
        ["Galletas"] = "cookie",
        ["Golosinas"] = "candy",
        ["Harinas"] = "wheat",
        ["Higiene Personal"] = "bath",
        ["Huevos"] = "egg",
        ["Lácteos"] = "milk",
        ["Legumbres"] = "bean",
        ["Limpieza"] = "spray-can",
        ["Mascotas"] = "dog",
        ["Otros"] = "package",
        ["Panificados"] = "croissant",
        ["Pastas"] = "utensils",
        ["Pescados y Mariscos"] = "fish",
        ["Pollo y Aves"] = "drumstick",
        ["Productos Dietéticos"] = "heart-pulse",
        ["Productos Sin TACC"] = "wheat-off",
        ["Repostería"] = "cake",
        ["Salsas y Aderezos"] = "bottle",
        ["Snacks"] = "cookie",
        ["Verduras"] = "carrot",
    };

    public static bool TryGetCategoryIcon(string categoriaNombre, out string icon)
        => CategoryIcons.TryGetValue(categoriaNombre, out icon!);

    /// <summary>
    /// Ícono final para un producto: por categoría si está mapeada, si no por palabra
    /// clave en el nombre.
    /// </summary>
    public static string Resolve(string? categoriaNombre, string productoNombre)
    {
        if (!string.IsNullOrWhiteSpace(categoriaNombre) && TryGetCategoryIcon(categoriaNombre, out var icon))
            return icon;

        return ResolveByKeyword(productoNombre).Icono;
    }

    public static (string CategoriaNombre, string? IconoSvg, string Icono) ResolveByKeyword(string productoNombre)
    {
        var normalizedName = NormalizeName(productoNombre);

        if (string.IsNullOrWhiteSpace(normalizedName))
            return ("Otros", "otros.svg", "package");

        if (ContainsAny(normalizedName, "leche", "queso", "yogur", "crema", "manteca", "lacteo", "ricota", "dulce de leche"))
            return ("Lácteos", "lacteos.svg", "milk");
        if (ContainsAny(normalizedName, "pollo", "ave", "gallina"))
            return ("Pollo y Aves", "pollo-aves.svg", "drumstick");
        if (ContainsAny(normalizedName, "cerdo", "bondiola", "panceta", "jamon", "chorizo", "salchicha"))
            return ("Carnes Porcinas", "carnes-porcinas.svg", "beef");
        if (ContainsAny(normalizedName, "pescado", "atun", "merluza", "marisco", "camaron", "langostino"))
            return ("Pescados y Mariscos", "pescados-mariscos.svg", "fish");
        if (ContainsAny(normalizedName, "carne", "vaca", "bife", "milanesa", "asado", "lomo", "nalga", "cuadril"))
            return ("Carnes Vacunas", "carnes-vacunas.svg", "beef");
        if (ContainsAny(normalizedName, "manzana", "banana", "naranja", "limon", "frutilla", "uva", "pera", "durazno", "cereza", "fruta"))
            return ("Frutas", "frutas.svg", "apple");
        if (ContainsAny(normalizedName, "ajo en polvo", "cebolla en polvo", "pimenton", "aji molido"))
            return ("Condimentos", "condimentos.svg", "leaf");
        if (ContainsAny(normalizedName, "tomate", "zanahoria", "cebolla", "lechuga", "papa", "batata", "morron", "ajo", "verdura", "zapallo", "espinaca", "acelga"))
            return ("Verduras", "verduras.svg", "carrot");
        if (ContainsAny(normalizedName, "lenteja", "garbanzo", "poroto", "arveja", "legumbre", "soja"))
            return ("Legumbres", "legumbres.svg", "bean");
        if (ContainsAny(normalizedName, "pan", "factura", "medialuna", "tostada", "galleta de agua"))
            return ("Panificados", "panificados.svg", "wheat");
        if (ContainsAny(normalizedName, "fideo", "pasta", "spaghetti", "raviol", "ñoqui", "noqui"))
            return ("Pastas", "pastas.svg", "utensils");
        if (ContainsAny(normalizedName, "arroz"))
            return ("Arroz", "arroz.svg", "wheat");
        if (ContainsAny(normalizedName, "cereal", "avena", "granola", "copos"))
            return ("Cereales", "cereales.svg", "wheat");
        if (ContainsAny(normalizedName, "harina", "maicena", "fecula"))
            return ("Harinas", "harinas.svg", "wheat");
        if (ContainsAny(normalizedName, "azucar", "edulcorante", "miel", "endulzante"))
            return ("Azúcar y Endulzantes", "azucar-endulzantes.svg", "candy");
        if (ContainsAny(normalizedName, "chocolate", "levadura", "esencia", "reposteria", "polvo de hornear", "coco rallado"))
            return ("Repostería", "reposteria.svg", "cake");
        if (ContainsAny(normalizedName, "aceite"))
            return ("Aceites", "aceites.svg", "droplet");
        if (ContainsAny(normalizedName, "sal", "pimienta", "oregano", "condimento", "especia", "caldo"))
            return ("Condimentos", "condimentos.svg", "leaf");
        if (ContainsAny(normalizedName, "salsa", "aderezo", "mayonesa", "ketchup", "mostaza", "vinagre", "aceto"))
            return ("Salsas y Aderezos", "salsas-aderezos.svg", "chef-hat");
        if (ContainsAny(normalizedName, "huevo"))
            return ("Huevos", "huevos.svg", "egg");
        if (ContainsAny(normalizedName, "cerveza", "vino", "fernet", "sidra", "whisky", "alcoholica"))
            return ("Bebidas Alcohólicas", "bebidas-alcoholicas.svg", "beer");
        if (ContainsAny(normalizedName, "agua", "jugo", "gaseosa", "soda", "coca", "bebida", "te", "cafe"))
            return ("Bebidas", "bebidas.svg", "glass-water");
        if (ContainsAny(normalizedName, "detergente", "jabón", "jabon", "limpieza", "lavandina", "desinfectante", "trapo", "esponja", "suavizante"))
            return ("Limpieza", "limpieza.svg", "spray-can");
        if (ContainsAny(normalizedName, "shampoo", "acondicionador", "papel higienico", "dentifrico", "desodorante", "baño", "bano", "jabon de tocador"))
            return ("Higiene Personal", "higiene-personal.svg", "bath");
        if (ContainsAny(normalizedName, "congelado", "hielo", "helado", "papas fritas congeladas"))
            return ("Congelados", "congelados.svg", "snowflake");

        return ("Otros", "otros.svg", "package");
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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

        return builder.ToString();
    }
}
