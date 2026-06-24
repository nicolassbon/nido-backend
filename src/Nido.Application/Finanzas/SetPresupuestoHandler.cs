namespace Nido.Application.Finanzas;

public sealed class SetPresupuestoHandler
{
    private readonly IFinanzasRepository _repo;

    public SetPresupuestoHandler(IFinanzasRepository repo) => _repo = repo;

    public async Task<PresupuestoResult> Handle(SetPresupuestoCommand command, CancellationToken ct)
    {
        var monto = await _repo.UpsertPresupuestoAsync(
            command.HogarId, command.Anio, command.Mes, command.Monto, ct);

        var primerDia = new DateOnly(command.Anio, command.Mes, 1);
        var ultimoDia = new DateOnly(command.Anio, command.Mes, DateTime.DaysInMonth(command.Anio, command.Mes));

        var gastos = await _repo.GetGastosAsync(
            new GetGastosQuery(command.HogarId, primerDia.ToString("yyyy-MM-dd"), ultimoDia.ToString("yyyy-MM-dd"), null), ct);

        return new PresupuestoResult(monto, gastos.TotalPeriodo, Math.Round(monto - gastos.TotalPeriodo, 2), command.Anio, command.Mes);
    }
}
