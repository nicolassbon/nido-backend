namespace Nido.Application.Finanzas;

public sealed record UpdateGastoCommand(
    Guid Id,
    Guid HogarId,
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid PagadoPorId
);
