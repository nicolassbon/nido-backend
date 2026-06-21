namespace Nido.Api.Contracts.Finanzas;

public sealed record GastoResponse(
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
