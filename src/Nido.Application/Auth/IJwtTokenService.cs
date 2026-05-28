namespace Nido.Application.Auth;

public interface IJwtTokenService
{
    string CreateToken(Guid usuarioId, Guid hogarId, string email);
}
