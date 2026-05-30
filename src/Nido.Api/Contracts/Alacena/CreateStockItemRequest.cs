namespace Nido.Api.Contracts.Alacena;

public sealed record CreateStockItemRequest(
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? FechaVencimiento,       // ISO yyyy-MM-dd or null
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    // TODO: remove these two fields and extract from JWT claims once auth is implemented
    Guid HogarId,
    Guid UsuarioId
);
