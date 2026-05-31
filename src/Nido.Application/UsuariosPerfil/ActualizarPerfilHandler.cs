
namespace Nido.Application.UsuariosPerfil;

public sealed class ActualizarPerfilHandler(UsuariosPerfil.IUsuarioRepository usuarioRepository)
{
    public async Task HandleAsync(UsuariosPerfil.ActualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.GetByIdAsync(command.UsuarioId, cancellationToken)
            ?? throw new Exception("Usuario no encontrado");

        usuario.Nombre = command.Nombre;
        usuario.Sexo = command.Sexo;
        usuario.FotoUrl = command.FotoUrl;

        await usuarioRepository.UpdateAsync(usuario, cancellationToken);
    }

}