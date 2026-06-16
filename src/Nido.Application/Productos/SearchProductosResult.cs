namespace Nido.Application.Productos;

public sealed record SearchProductosResult(
    string  Nombre,
    string? CategoriaNombre,
    Guid?   CategoriaId,
    string? UnidadMedida,
    string? Ubicacion
);
