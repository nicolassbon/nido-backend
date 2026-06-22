namespace Nido.Api.Contracts.Productos;

public sealed record CreateStockHomeRequest(
    string Nombre,
    Guid? CategoriaId,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    // Cantidad de envases idénticos. Default 1.
    int? CantidadEnvases = 1,
    // Información nutricional por 100 g (del escaneo a Open Food Facts).
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
