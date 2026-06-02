using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Nido.Infrastructure.Auth;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private readonly IOptions<JwtOptions> _jwtOptions = Options.Create(new JwtOptions
    {
        Key = "local-dev-super-secret-key-for-tests-only",
        Issuer = "nido-api",
        Audience = "nido-clients",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryDays = 7
    });

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyBase64String()
    {
        var service = new JwtTokenService(_jwtOptions);

        var token = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void HashRefreshToken_SameInput_ReturnsConsistentHash()
    {
        var service = new JwtTokenService(_jwtOptions);
        var token = service.GenerateRefreshToken();

        var hash1 = service.HashRefreshToken(token);
        var hash2 = service.HashRefreshToken(token);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex = 64 chars
    }

    [Fact]
    public void CreateAuthTokens_ReturnsAccessTokenAndRefreshToken()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var (accessToken, refreshToken, refreshTokenExpiresAt) = service.CreateAuthTokens(usuarioId, hogarId, "test@mail.com", "Test");

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.True(refreshTokenExpiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        Assert.Equal("nido-api", jwt.Issuer);
        Assert.Equal("nido-clients", jwt.Audiences.First());
        Assert.Equal(usuarioId.ToString(), jwt.Subject);

        var emailClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.Equal("test@mail.com", emailClaim.Value);
    }

    [Fact]
    public void CreateToken_UsesConfiguredExpiryMinutes()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var token = service.CreateToken(usuarioId, hogarId, "test@mail.com", "Test");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var expiry = jwt.ValidTo;
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        Assert.True(expiry > DateTime.UtcNow.AddMinutes(55) && expiry <= DateTime.UtcNow.AddMinutes(65),
            $"Expected expiry around {expectedExpiry}, but got {expiry}");
    }
}
