namespace Nido.Api.Contracts.Alacena;

public sealed record UpdateStockItemRequest(
    decimal? Cantidad,
    string? Ubicacion,
    string? FechaVencimiento,
    bool? EstaAbierto,
    decimal? PorcentajeConsumido
);
