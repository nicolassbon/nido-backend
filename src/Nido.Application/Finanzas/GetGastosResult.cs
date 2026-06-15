namespace Nido.Application.Finanzas;

public sealed record GetGastosResult(
    IReadOnlyList<GastoResult> Gastos,
    decimal TotalPeriodo
);
