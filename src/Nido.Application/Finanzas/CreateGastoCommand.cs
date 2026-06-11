namespace Nido.Application.Finanzas;

public sealed record CreateGastoCommand(
    Guid HogarId,
    Guid PagadoPorId,
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha
);
