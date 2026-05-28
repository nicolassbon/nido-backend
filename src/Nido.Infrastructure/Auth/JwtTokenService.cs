using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    public string CreateToken(Guid usuarioId, Guid hogarId, string email)
    {
        var key = _configuration["Jwt:Key"] ?? "local-dev-super-secret-key-for-tests-only";
        var issuer = _configuration["Jwt:Issuer"] ?? "nido-api";
        var audience = _configuration["Jwt:Audience"] ?? "nido-clients";

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.UsuarioId, usuarioId.ToString()),
            new Claim(Application.Common.Security.ClaimTypes.HogarId, hogarId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
