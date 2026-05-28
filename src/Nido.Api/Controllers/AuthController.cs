using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Auth;
using Nido.Application.Auth;

namespace Nido.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;

    public AuthController(RegisterUserHandler registerUserHandler)
    {
        _registerUserHandler = registerUserHandler;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerUserHandler.Handle(
            new RegisterUserCommand(request.Nombre, request.Email, request.Password, request.Sexo, request.FotoUrl),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }
}
