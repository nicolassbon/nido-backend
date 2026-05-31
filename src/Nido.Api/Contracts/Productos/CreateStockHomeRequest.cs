namespace Nido.Api.Contracts.Productos;

public sealed record CreateStockHomeRequest(
    Guid CategoriaId,
    Guid ProductoId,
    decimal CantidadActual,
    string? UnidadMedida,
    DateTime? FechaVencimiento,
    Guid HogarId,
    Guid UsuarioId,
    string Ubicacion,
    bool EstaAbierto,
    decimal PorcentajeConsumido
);