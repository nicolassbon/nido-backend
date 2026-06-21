namespace Nido.Application.Alacena;

public sealed record StockItemResult(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    string? Imagen,
    string? CodigoBarras,
    string? CategoriaNombre,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    int CantidadEnvases,
    string OrigenCarga = StockLoadOrigins.Manual,
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
