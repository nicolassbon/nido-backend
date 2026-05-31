namespace Nido.Application.Auth;

public interface IJwtTokenService
{
    string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    (string AccessToken, string RefreshToken) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre);
}
