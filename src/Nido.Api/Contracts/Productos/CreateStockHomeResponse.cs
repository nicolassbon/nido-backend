namespace Nido.Api.Contracts.Productos;

public sealed record CreateStockHomeResponse(
   Guid StockHogarId,
    Guid ProductoId,
    decimal CantidadActual,
    string UnidadMedida,
    DateTime? FechaVencimiento,
    Guid UsuarioIngresoId,
    string Ubicacion,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    Guid CategoriaId,
    int CantidadEnvases
);