using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Payments;

namespace Nido.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre)
        => CreateToken(usuarioId, hogarId, email, nombre, new HouseholdEntitlement(HouseholdPlan.Free, SubscriptionStatus.None, null));

    public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan)
        => CreateToken(usuarioId, hogarId, email, nombre, new HouseholdEntitlement(plan, SubscriptionStatus.None, null));

    public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.UsuarioId, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.HogarId, hogarId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(Application.Common.Security.ClaimTypes.Nombre, nombre),
            new Claim(Application.Common.Security.ClaimTypes.Plan, entitlement.Plan.ToJwtClaimString()),
            new Claim(Application.Common.Security.ClaimTypes.SubscriptionStatus, entitlement.SubscriptionStatus.ToJwtClaimString())
        };

        if (entitlement.TrialEndsAt.HasValue)
        {
            claims.Add(new Claim(Application.Common.Security.ClaimTypes.TrialEndsAt, entitlement.TrialEndsAt.Value.ToString("O")));
        }

        if (entitlement.SubscriptionEndsAt.HasValue)
        {
            claims.Add(new Claim(Application.Common.Security.ClaimTypes.SubscriptionEndsAt, entitlement.SubscriptionEndsAt.Value.ToString("O")));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(
        Guid usuarioId, Guid hogarId, string email, string nombre)
        => CreateAuthTokens(usuarioId, hogarId, email, nombre, new HouseholdEntitlement(HouseholdPlan.Free, SubscriptionStatus.None, null));

    public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(
        Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan)
        => CreateAuthTokens(usuarioId, hogarId, email, nombre, new HouseholdEntitlement(plan, SubscriptionStatus.None, null));

    public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(
        Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
    {
        var accessToken = CreateToken(usuarioId, hogarId, email, nombre, entitlement);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);
        return (accessToken, refreshToken, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
