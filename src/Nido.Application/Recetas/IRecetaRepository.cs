namespace Nido.Application.Recetas;

public interface IRecetaRepository
{
    Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, Guid usuarioId, CancellationToken ct);
    Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct);
    Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct);
}
