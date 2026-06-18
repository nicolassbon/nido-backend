namespace Nido.Application.Finanzas;

public interface IFinanzasRepository
{
    Task<GastoResult> CreateGastoAsync(CreateGastoCommand command, CancellationToken ct);
    Task<GetGastosResult> GetGastosAsync(GetGastosQuery query, CancellationToken ct);
    Task<GetBalanceResult> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct);
    Task<bool?> GetModoAhorroAsync(Guid hogarId, CancellationToken ct);
    Task<bool> ToggleModoAhorroAsync(Guid hogarId, bool activo, CancellationToken ct);
    Task<FacturaResult> CreateFacturaAsync(CreateFacturaCommand command, Guid facturaId, string? storageKey, CancellationToken ct);
    Task<IReadOnlyList<FacturaResult>> GetFacturasAsync(GetFacturasQuery query, CancellationToken ct);
    Task<FacturaResult?> MarcarPagadaAsync(Guid facturaId, Guid hogarId, CancellationToken ct);
    Task<string?> DeleteFacturaAsync(Guid facturaId, Guid hogarId, CancellationToken ct);
    Task<GastoResult?> UpdateGastoAsync(UpdateGastoCommand command, CancellationToken ct);
    Task<DeleteGastoResult?> DeleteGastoAsync(Guid gastoId, Guid hogarId, CancellationToken ct);
    Task<decimal?> GetPresupuestoAsync(Guid hogarId, int anio, int mes, CancellationToken ct);
    Task<decimal> UpsertPresupuestoAsync(Guid hogarId, int anio, int mes, decimal monto, CancellationToken ct);
}
