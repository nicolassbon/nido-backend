namespace Nido.Api.Contracts.Alacena;

public sealed record StockMovementResponse(
    Guid Id,
    Guid? ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    string? UnidadMedida,
    string Motivo,
    DateTime FechaConsumo,
    Guid? UsuarioId
);
