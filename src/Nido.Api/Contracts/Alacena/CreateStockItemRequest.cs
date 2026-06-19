namespace Nido.Api.Contracts.Alacena;

public sealed record CreateStockItemRequest(
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    // Cantidad de envases idénticos. Default 1.
    int? CantidadEnvases = 1,
    string? OrigenCarga = null
);
