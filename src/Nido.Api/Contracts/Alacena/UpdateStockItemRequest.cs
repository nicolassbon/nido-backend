namespace Nido.Api.Contracts.Alacena;

public sealed record UpdateStockItemRequest(
    string? Nombre,
    decimal? Cantidad,
    string? Ubicacion,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool? EstaAbierto,
    decimal? PorcentajeConsumido,
    // Cantidad de envases idénticos. Null = no actualizar.
    int? CantidadEnvases = null
);
