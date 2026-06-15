namespace Nido.Api.Contracts.Finanzas;

public sealed record RecetaRecomendadaResponse(
    Guid Id,
    string Nombre,
    string? ImagenUrl,
    int IngredientesEnStock,
    int TotalIngredientes
);

public sealed record RecomendacionesResponse(
    IReadOnlyList<RecetaRecomendadaResponse> Recetas,
    IReadOnlyList<string> Tips
);
