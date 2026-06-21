using Nido.Domain.Usuarios;

namespace Nido.Application.UsuariosPerfil;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);
    Task<PerfilStatsResult> GetStatsAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRestriccionesUsuarioAsync(Guid usuarioId, string tipo, CancellationToken cancellationToken);
    Task ReplaceRestriccionesUsuarioAsync(Guid usuarioId, string tipo, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken);
}
