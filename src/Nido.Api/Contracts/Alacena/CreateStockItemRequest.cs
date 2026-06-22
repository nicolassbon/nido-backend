namespace Nido.Api.Contracts.Alacena;

public sealed record CreateStockItemRequest(
    string Nombre,
    Guid? CategoriaId,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    // Cantidad de envases idénticos. Default 1.
    int? CantidadEnvases = 1,
    string? OrigenCarga = null,
    // Información nutricional por 100 g (del escaneo a Open Food Facts).
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
