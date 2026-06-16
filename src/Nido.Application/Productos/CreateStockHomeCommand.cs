namespace Nido.Application.Productos;

public sealed record CreateStockHomeCommand(
    string Nombre,
    Guid? CategoriaId,
    string Ubicacion,
    decimal CantidadActual,
    string UnidadMedida,
    string? FechaVencimiento,
    Guid HogarId,
    Guid UsuarioIngresoId,
    int CantidadEnvases = 1
);
