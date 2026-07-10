namespace Nido.Api.Contracts.Productos;

public sealed record GetProductManualResponse(
    Guid StockHogarId,
    Guid ProductoId,
    string Nombre,
    Guid? CategoriaId,
    string? CategoriaNombre,
    string? CodigoBarras,
    string? ImagenUrl,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    int CantidadEnvases,
    string? IconoSvg = null,
    string? Icono = null,
    decimal? CantidadCompraEstandar = null,
    string? UnidadCompraEstandar = null
);
