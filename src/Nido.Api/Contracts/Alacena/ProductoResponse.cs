namespace Nido.Api.Contracts.Alacena;

/// <summary>
/// Returned by GET /api/productos/barcode/{barcode}.
/// The frontend checks this before calling Open Food Facts.
/// </summary>
public sealed record ProductoResponse(
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string? CategoriaNombre,
    int? TtlDias,            // category-level TTL hint in days
    // Datos de la última compra del producto en el hogar (pre-llenan el re-escaneo).
    decimal? Gramaje = null,
    string? UnidadMedida = null,
    // Información nutricional por 100 g (si está guardada).
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
