namespace Nido.Application.Recetas;

public sealed record NotaItem(
    Guid     Id,
    Guid     RecetaId,
    Guid     HogarId,
    Guid     UsuarioId,
    string   UsuarioNombre,
    string?  UsuarioFotoUrl,
    string   Texto,
    DateTime CreatedAt
);

public interface INotaRecetaRepository
{
    Task<NotaItem> AddAsync(Guid recetaId, Guid hogarId, Guid usuarioId, string texto, CancellationToken ct);
    Task<IReadOnlyList<NotaItem>> GetByRecetaAndHogarAsync(Guid recetaId, Guid hogarId, CancellationToken ct);
    Task DeleteAsync(Guid notaId, Guid hogarId, Guid usuarioId, CancellationToken ct);
}
