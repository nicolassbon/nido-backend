namespace Nido.Application.Finanzas;

public sealed record SetPresupuestoCommand(Guid HogarId, decimal Monto, int Anio, int Mes);
