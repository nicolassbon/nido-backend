namespace Nido.Application.Productos;

public sealed record CreateStockHomeCommand(
    Guid CategoriaId,
    Guid ProductoId,
    decimal CantidadActual,
    string UnidadMedida,
    DateTime? FechaVencimiento,
    Guid HogarId,
    Guid UsuarioIngresoId,
    string Ubicaciom,
    bool estaAbierto,
    decimal porcentajeConsumido

);