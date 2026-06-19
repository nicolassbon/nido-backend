namespace Nido.Api.Contracts.Finanzas;

public sealed record GastosListResponse(
    IReadOnlyList<GastoResponse> Gastos,
    decimal TotalPeriodo
);
