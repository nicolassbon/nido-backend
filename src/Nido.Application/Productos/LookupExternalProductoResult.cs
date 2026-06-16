namespace Nido.Application.Productos;

public sealed record LookupExternalProductoResult(
    string   Name,
    string   Image,
    string   Brands,
    string[] CategoriesTags,
    /// <summary>
    /// Categoría canónica de Nido sugerida a partir de CategoriesTags.
    /// Uno de: "General", "Lácteos", "Bebidas", "Congelados", "Despensa".
    /// </summary>
    string   CategoriaSugerida,
    bool     FoundInDb
);
