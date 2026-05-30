namespace Nido.Application.Alacena;

public sealed record UpdateStockItemRequestModel(
    Guid Id,
    Guid UsuarioId,
    decimal? Cantidad,
    string? Ubicacion,
    string? FechaVencimiento,
    bool? EstaAbierto,
    decimal? PorcentajeConsumido
);
