namespace Nido.Api.Contracts.Productos;

public sealed record CreateStockHomeRequest(
    string Nombre,
    Guid CategoriaId,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    DateTime? FechaVencimiento,
    // Cantidad de envases idénticos. Default 1.
    int? CantidadEnvases = 1
);
