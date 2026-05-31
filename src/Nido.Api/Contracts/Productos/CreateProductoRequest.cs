namespace Nido.Api.Contracts.Productos;

public sealed record CreateProductoRequest(
    string? Nombre,
    Guid CategoriaId,
    decimal Cantidad,
    string? UnidadMedida,
    DateTime? FechaVencimiento,
    Guid HogarId,
    Guid UsuarioId
);