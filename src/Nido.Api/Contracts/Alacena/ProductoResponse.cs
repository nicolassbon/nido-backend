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
    int? TtlDias             // category-level TTL hint in days
);
