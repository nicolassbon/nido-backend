namespace Nido.Application.Finanzas;

public sealed record GetBalanceResult(
    IReadOnlyList<BalanceMiembroResult> Miembros,
    decimal TotalPeriodo
);
