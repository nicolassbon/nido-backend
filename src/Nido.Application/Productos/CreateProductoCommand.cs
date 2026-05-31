namespace Nido.Application.Productos;

public sealed record CreateProductoCommand(
    string Nombre,
    Guid CategoriaId,
    decimal Cantidad,
    string UnidadMedida,
    DateTime? FechaVencimiento,
    Guid HogarId,
    Guid UsuarioIngresoId

);