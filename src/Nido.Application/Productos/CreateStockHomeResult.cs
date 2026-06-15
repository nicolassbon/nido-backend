namespace Nido.Application.Productos;

public sealed record CreateStockHomeResult(
    Guid StockHogarId,
    Guid ProductoId,
    decimal CantidadActual,
    string UnidadMedida,
    string? FechaVencimiento,
    Guid UsuarioIngresoId,
    string Ubicacion,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    Guid? CategoriaId
    
);
