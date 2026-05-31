using Nido.Application.Auth.RefreshToken;

namespace Nido.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre);
}
