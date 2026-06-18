namespace Nido.Api.Contracts.Finanzas;

public sealed record PresupuestoResponse(
    decimal? Monto,
    decimal GastoActual,
    decimal? Restante,
    int Anio,
    int Mes
);

public sealed record SetPresupuestoRequest(decimal Monto);
