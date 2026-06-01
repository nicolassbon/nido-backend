namespace Nido.Application.Recetas;

public interface IRecetaRepository
{
    Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, CancellationToken ct);
    Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct);
}
