
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.UsuariosPerfil;

public sealed class ActualizarPerfilHandler(
    UsuariosPerfil.IUsuarioRepository usuarioRepository,
    IProfileImageProcessor profileImageProcessor,
    IProfileImageStorage profileImageStorage)
{
    public async Task HandleAsync(UsuariosPerfil.ActualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.GetByIdAsync(command.UsuarioId, cancellationToken)
            ?? throw new Exception("Usuario no encontrado");

        string? newFotoStorageKey = usuario.FotoStorageKey;

        if (command.Foto is not null)
        {
            var processed = await profileImageProcessor.ProcessAsync(command.Foto, cancellationToken);
            var storageKey = $"usuarios/{usuario.Id}/profile/{Guid.NewGuid():N}.webp";
            await profileImageStorage.UploadAsync(storageKey, processed.Content, processed.ContentType, cancellationToken);

            if (!string.IsNullOrWhiteSpace(usuario.FotoStorageKey))
            {
                try
                {
                    await profileImageStorage.DeleteAsync(usuario.FotoStorageKey, CancellationToken.None);
                }
                catch
                {
                    // Ignore or log error
                }
            }

            newFotoStorageKey = storageKey;
        }

        usuario.ActualizarPerfil(command.Nombre, command.Sexo, command.Telefono, newFotoStorageKey);

        await usuarioRepository.UpdateAsync(usuario, cancellationToken);
    }
}