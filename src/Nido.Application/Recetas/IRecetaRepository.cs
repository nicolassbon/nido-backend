namespace Nido.Application.Recetas;

public interface IRecetaRepository
{
    Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<RecetaResult>> GetSavedAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RecetaResult>>(Array.Empty<RecetaResult>());
    Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct);
    Task<bool> SaveAsync(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct)
        => Task.FromResult(false);
    Task<bool> UnsaveAsync(Guid recetaId, Guid hogarId, CancellationToken ct)
        => Task.FromResult(false);
    Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct);
}
