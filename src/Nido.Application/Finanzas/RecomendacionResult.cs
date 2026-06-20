namespace Nido.Application.Finanzas;

public sealed record RecetaRecomendadaResult(
    Guid Id,
    string Nombre,
    string? ImagenUrl,
    int IngredientesEnStock,
    int TotalIngredientes
);
