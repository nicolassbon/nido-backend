namespace Nido.Application.Alacena;

public sealed record CreateStockItemRequestModel(
    Guid HogarId,
    Guid UsuarioId,
    string Nombre,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido
);
