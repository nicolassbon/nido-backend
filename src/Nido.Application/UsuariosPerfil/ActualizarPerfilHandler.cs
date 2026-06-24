
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Storage;

namespace Nido.Application.UsuariosPerfil;

public sealed class ActualizarPerfilHandler(
    UsuariosPerfil.IUsuarioRepository usuarioRepository,
    IProfileImageProcessor profileImageProcessor,
    IFileStorageService fileStorageService,
    StorageKeyFactory storageKeyFactory)
{
    public async Task HandleAsync(UsuariosPerfil.ActualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.GetByIdAsync(command.UsuarioId, cancellationToken)
            ?? throw new Exception("Usuario no encontrado");

        string? newFotoStorageKey = usuario.FotoStorageKey;
        DateTime? fotoUpdatedAt = usuario.FotoUpdatedAt;
        string? oldFotoStorageKey = null;

        if (command.Foto is not null)
        {
            var processed = await profileImageProcessor.ProcessAsync(command.Foto, cancellationToken);
            var storageKey = storageKeyFactory.ForAvatar(usuario.Id);

            await using var stream = new MemoryStream(processed.Content);
            await fileStorageService.UploadAsync(stream, storageKey, processed.ContentType, cancellationToken);

            oldFotoStorageKey = usuario.FotoStorageKey;

            newFotoStorageKey = storageKey;
            fotoUpdatedAt = DateTime.UtcNow;
        }

        usuario.ActualizarPerfil(command.Nombre, command.Sexo, command.Telefono, newFotoStorageKey, fotoUpdatedAt);

        await usuarioRepository.UpdateAsync(usuario, cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldFotoStorageKey))
        {
            try
            {
                await fileStorageService.DeleteAsync(oldFotoStorageKey, CancellationToken.None);
            }
            catch
            {
                // Non-critical cleanup failure — log in future
            }
        }
    }
}
