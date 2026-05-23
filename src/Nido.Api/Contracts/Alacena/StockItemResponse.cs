namespace Nido.Api.Contracts.Alacena;

public sealed record StockItemResponse(
    Guid    Id,
    Guid    ProductoId,
    string  Nombre,
    string? Imagen,
    string? CodigoBarras,
    string  Ubicacion,
    decimal Cantidad,
    string? FechaVencimiento,       // ISO yyyy-MM-dd or null
    bool    EstaAbierto,
    decimal PorcentajeConsumido
);
