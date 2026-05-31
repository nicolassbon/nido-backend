namespace Nido.Api.Contracts.Productos;

public sealed record CreateProductoResponse(
    Guid ProductoId,
    string Nombre,
    decimal Cantidad,
    string UnidadMedida
);