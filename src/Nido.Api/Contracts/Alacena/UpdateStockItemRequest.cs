namespace Nido.Api.Contracts.Alacena;

public sealed record UpdateStockItemRequest(
    decimal? Cantidad,
    string?  Ubicacion,
    string?  FechaVencimiento,
    bool?    EstaAbierto,
    decimal? PorcentajeConsumido,
    // TODO: extract from JWT claims once auth is implemented
    Guid UsuarioId
);
