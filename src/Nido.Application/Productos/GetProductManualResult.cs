namespace Nido.Application.Productos;

public sealed record GetProductManualResult(
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
    decimal PorcentajeConsumido
);