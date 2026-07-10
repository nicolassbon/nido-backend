using Nido.Application.Auth.RefreshToken;
using Nido.Application.Payments;

namespace Nido.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre);
    string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan);
    string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre);
    (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan);
    (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement);
}
