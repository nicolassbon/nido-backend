namespace Nido.Application.Alacena;

public sealed record UpdateStockItemRequestModel(
    Guid Id,
    Guid UsuarioId,
    Guid HogarId,
    string? Nombre,
    decimal? Cantidad,
    string? Ubicacion,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool? EstaAbierto,
    decimal? PorcentajeConsumido
);
