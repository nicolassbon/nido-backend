namespace Nido.Application.Finanzas;

public sealed record RecomendacionResult(
    IReadOnlyList<RecetaRecomendadaResult> Recetas,
    IReadOnlyList<string> Tips
);

public sealed record RecetaRecomendadaResult(
    Guid Id,
    string Nombre,
    string? ImagenUrl,
    int IngredientesEnStock,
    int TotalIngredientes
);
