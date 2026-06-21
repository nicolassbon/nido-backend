namespace Nido.Application.Finanzas;

public sealed record GastoResult(
    Guid Id,
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid PagadoPorId,
    string PagadoPorNombre,
    DateTime CreatedAt,
    Guid? FacturaId
);
