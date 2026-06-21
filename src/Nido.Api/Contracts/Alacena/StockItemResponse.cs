namespace Nido.Api.Contracts.Alacena;

public sealed record StockItemResponse(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    string? Imagen,
    string? CodigoBarras,
    string? CategoriaNombre,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,       // ISO yyyy-MM-dd or null
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    // Cantidad de envases idénticos del mismo producto.
    int CantidadEnvases,
    string OrigenCarga,
    // Información nutricional por 100 g (null si el producto no la tiene).
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
