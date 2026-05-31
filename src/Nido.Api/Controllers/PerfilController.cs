using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.UsuariosPerfil;
using Nido.Application.UsuariosPerfil;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/perfiles")]
public sealed class PerfilController(ActualizarPerfilHandler handler) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> ActualizarPerfil(
        [FromBody] ActualizarPerfilRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new ActualizarPerfilCommand(
            request.UsuarioId,
            request.Nombre,
            request.Sexo,
            request.FotoUrl
        );

        await handler.HandleAsync(command, cancellationToken);

       
        return Ok(new { message = "Perfil actualizado con éxito." });
    }
}