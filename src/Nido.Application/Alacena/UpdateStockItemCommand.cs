namespace Nido.Application.Alacena;

public sealed record UpdateStockItemCommand(
    Guid Id,
    Guid UsuarioId,
    string? Nombre,
    decimal? Cantidad,
    string? Ubicacion,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool? EstaAbierto,
    decimal? PorcentajeConsumido,
    int? CantidadEnvases = null
);
