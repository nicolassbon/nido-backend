namespace Nido.Api.Contracts.Finanzas;

public sealed record UpdateGastoRequest(
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid? PagadoPorId
);

public sealed record DeleteGastoResponse(bool FacturaRevertida, Guid? FacturaId);
