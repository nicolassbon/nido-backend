using Nido.Domain.Usuarios;

namespace Nido.Application.UsuariosPerfil;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);
}