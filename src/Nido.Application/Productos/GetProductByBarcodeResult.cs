namespace Nido.Application.Productos;

public sealed record GetProductByBarcodeResult(
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string? CategoriaNombre,
    int? TtlDias,
    // Datos de la última compra del producto en el hogar (para pre-llenar el re-escaneo).
    decimal? Gramaje = null,
    string? UnidadMedida = null,
    // Información nutricional por 100 g (si el producto la tiene guardada).
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
