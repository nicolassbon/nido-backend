namespace Nido.Application.Finanzas;

public sealed record PresupuestoResult(
    decimal? Monto,
    decimal GastoActual,
    decimal? Restante,
    int Anio,
    int Mes
);
