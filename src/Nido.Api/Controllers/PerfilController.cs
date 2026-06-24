using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.UsuariosPerfil;
using Nido.Application.Common.Security;
using Nido.Application.UsuariosPerfil;
using Nido.Application.Common.ProfileImages;
using Microsoft.Extensions.Options;
using Nido.Infrastructure.ProfileImages;
using Nido.Application.Auth.Register;
using Nido.Application.Auth.Interfaces;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/perfiles")]
public sealed class PerfilController(
    ActualizarPerfilHandler handler,
    IUsuarioRepository repository,
    IAuthRepository authRepository,
    ICurrentUserContext currentUser,
    IOptions<ProfileImageOptions> profileImageOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ObtenerMiPerfil(
        [FromServices] IProfileImagePublicUrlResolver urlResolver,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UsuarioId;

        var usuario = await repository.GetByIdAsync(userId, cancellationToken);
        var authUser = await authRepository.FindByIdAsync(userId, cancellationToken);

        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var fotoUrl = !string.IsNullOrWhiteSpace(usuario.FotoStorageKey)
            ? urlResolver.Resolve(usuario.FotoStorageKey, usuario.FotoUpdatedAt)
            : null;

        var alergias = await repository.GetRestriccionesUsuarioAsync(userId, "alergia", cancellationToken);
        var alimentacion = await repository.GetRestriccionesUsuarioAsync(userId, "restriccion_alimentaria", cancellationToken);
        var stats = await repository.GetStatsAsync(userId, currentUser.HogarId, cancellationToken);

        return Ok(new
        {
            nombre = usuario.Nombre,
            email = usuario.Email,
            hasPassword = !string.IsNullOrWhiteSpace(authUser?.PasswordHash),
            hasGoogleLinked = string.Equals(authUser?.OauthProvider, "google", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(authUser?.OauthId),
            sexo = usuario.Sexo,
            telefono = usuario.Telefono,
            fotoUrl,
            fechaRegistro = usuario.CreatedAt.ToString("dd/MM/yyyy"),
            alergias,
            alimentacion,
            tareasCompletadas = stats.TareasCompletadas,
            productosEscaneados = stats.ProductosEscaneados,
            logros = stats.Logros
        });
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ActualizarPerfil(
        [FromForm] ActualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        RegistrationProfileImageUpload? foto = null;
        if (request.Foto is not null)
        {
            if (request.Foto.Length == 0)
            {
                throw new ArgumentException("Empty file is not allowed for profile image.");
            }

            var maxSizeInBytes = profileImageOptions.Value.MaxBytes;
            if (request.Foto.Length > maxSizeInBytes)
            {
                throw new ArgumentException("Profile image exceeds the allowed limit.");
            }

            await using var memoryStream = new MemoryStream();
            await request.Foto.CopyToAsync(memoryStream, cancellationToken);

            foto = new RegistrationProfileImageUpload(
                request.Foto.FileName,
                request.Foto.ContentType,
                memoryStream.ToArray());
        }

        var command = new ActualizarPerfilCommand(
            currentUser.UsuarioId,
            request.Nombre,
            request.Sexo,
            request.Telefono,
            foto
        );

        await handler.HandleAsync(command, cancellationToken);


        return Ok(new { message = "Perfil actualizado con éxito." });
    }

    [HttpPut("restricciones")]
    public async Task<IActionResult> ActualizarRestricciones(
        [FromBody] ActualizarRestriccionesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UsuarioId;
        await repository.ReplaceRestriccionesUsuarioAsync(userId, request.Tipo, request.RestriccionIds, cancellationToken);
        return Ok(new { message = "Restricciones actualizadas con éxito." });
    }
}
