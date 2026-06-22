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
    bool     FoundInDb,
    // Información nutricional por 100 g (de Open Food Facts). Null cuando la
    // fuente no la provee.
    decimal? Calorias      = null,
    decimal? Proteinas     = null,
    decimal? Carbohidratos = null,
    decimal? Grasas        = null,
    // Gramaje extraído del nombre del producto (ej: "290" de "Producto 290g")
    decimal? GramajeExtraido = null
);
