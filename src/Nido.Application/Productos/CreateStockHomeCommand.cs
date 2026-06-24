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
    int CantidadEnvases = 1,
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null
);
