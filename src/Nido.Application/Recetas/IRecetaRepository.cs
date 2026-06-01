namespace Nido.Application.Recetas;

public interface IRecetaRepository
{
    Task<IReadOnlyList<RecetaResult>> GetAllAsync(CancellationToken ct);
}
