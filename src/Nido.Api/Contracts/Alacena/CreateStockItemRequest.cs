namespace Nido.Api.Contracts.Alacena;

public sealed record CreateStockItemRequest(
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido
);
