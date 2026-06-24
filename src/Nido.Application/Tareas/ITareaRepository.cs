namespace Nido.Application.Tareas;

public interface ITareaRepository
{
    Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct);
    Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct);
    Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct);
    Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct);
    Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion, DateTime? fechaLimite, string? estado, CancellationToken ct);
    Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct);
    Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct);
    Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct);
}
