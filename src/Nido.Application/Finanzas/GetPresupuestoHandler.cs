namespace Nido.Application.Finanzas;

public sealed class GetPresupuestoHandler
{
    private readonly IFinanzasRepository _repo;

    public GetPresupuestoHandler(IFinanzasRepository repo) => _repo = repo;

    public async Task<PresupuestoResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var primerDia = new DateOnly(now.Year, now.Month, 1);
        var ultimoDia = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

        var gastos = await _repo.GetGastosAsync(
            new GetGastosQuery(hogarId, primerDia.ToString("yyyy-MM-dd"), ultimoDia.ToString("yyyy-MM-dd"), null), ct);

        var monto = await _repo.GetPresupuestoAsync(hogarId, now.Year, now.Month, ct);

        decimal? restante = monto.HasValue ? Math.Round(monto.Value - gastos.TotalPeriodo, 2) : null;

        return new PresupuestoResult(monto, gastos.TotalPeriodo, restante, now.Year, now.Month);
    }
}
