namespace Nido.Application.Productos;

public sealed record GetProductByBarcodeResult(
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string? CategoriaNombre,
    int? TtlDias
);
