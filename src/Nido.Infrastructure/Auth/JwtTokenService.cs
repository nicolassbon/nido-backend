using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nido.Application.Auth;
using Nido.Application.Common.Security;

namespace Nido.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set the 'Jwt__Key' environment variable or 'Jwt:Key' in appsettings.");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "nido-api";
        var audience = _configuration["Jwt:Audience"] ?? "nido-clients";
        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var parsed) ? parsed : 60;

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.UsuarioId, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.HogarId, hogarId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(Application.Common.Security.ClaimTypes.Nombre, nombre)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    public (string AccessToken, string RefreshToken) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
    {
        var accessToken = CreateToken(usuarioId, hogarId, email, nombre);
        var refreshToken = GenerateRefreshToken();
        return (accessToken, refreshToken);
    }
}
