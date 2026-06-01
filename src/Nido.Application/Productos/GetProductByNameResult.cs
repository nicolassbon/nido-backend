namespace Nido.Application.Productos;

public sealed record GetProductByNameResult(
    Guid Id,
    string Nombre,
    Guid? CategoriaId,
    string? ImagenUrl
);
