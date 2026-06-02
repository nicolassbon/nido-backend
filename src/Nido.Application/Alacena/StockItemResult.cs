namespace Nido.Application.Alacena;

public sealed record StockItemResult(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    string? Imagen,
    string? CodigoBarras,
    string Ubicacion,
    decimal Cantidad,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido
);
