namespace Nido.Api.Contracts.Finanzas;

public sealed record CreateGastoRequest(
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid? PagadoPorId
);
