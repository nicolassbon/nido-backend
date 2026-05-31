namespace Nido.Application.Productos;

public sealed record CreateProductoResult(
    Guid ProductoId,
    Guid StockHogarId,
    string Nombre,
    decimal Cantidad,
    string UnidadMedida
);