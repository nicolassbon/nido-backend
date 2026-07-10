using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Nido.Application.Payments;
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

    [Fact]
    public void CreateToken_WithPremiumPlan_IncludesHogarPlanClaim()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var token = service.CreateToken(usuarioId, hogarId, "test@mail.com", "Test", HouseholdPlan.Premium);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var planClaim = jwt.Claims.First(c => c.Type == "plan");
        Assert.Equal("Hogar", planClaim.Value);
    }

    [Fact]
    public void CreateToken_WithFreePlan_IncludesBasicoPlanClaim()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var token = service.CreateToken(usuarioId, hogarId, "test@mail.com", "Test", HouseholdPlan.Free);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var planClaim = jwt.Claims.First(c => c.Type == "plan");
        Assert.Equal("Básico", planClaim.Value);
    }

    [Fact]
    public void CreateAuthTokens_WithPremiumPlan_PassesPlanToAccessToken()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var (accessToken, _, _) = service.CreateAuthTokens(usuarioId, hogarId, "test@mail.com", "Test", HouseholdPlan.Premium);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var planClaim = jwt.Claims.First(c => c.Type == "plan");
        Assert.Equal("Hogar", planClaim.Value);
    }

    [Fact]
    public void CreateToken_WithEntitlement_IncludesSubscriptionClaims()
    {
        var service = new JwtTokenService(_jwtOptions);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var trialEndsAt = new DateTime(2026, 7, 20, 23, 59, 59, DateTimeKind.Utc);
        var subscriptionEndsAt = new DateTime(2026, 8, 1, 23, 59, 59, DateTimeKind.Utc);
        var entitlement = new HouseholdEntitlement(HouseholdPlan.Premium, SubscriptionStatus.Active, trialEndsAt, subscriptionEndsAt);

        var token = service.CreateToken(usuarioId, hogarId, "test@mail.com", "Test", entitlement);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("Hogar", jwt.Claims.First(c => c.Type == "plan").Value);
        Assert.Equal("active", jwt.Claims.First(c => c.Type == "subscriptionStatus").Value);
        Assert.Equal(trialEndsAt.ToString("O"), jwt.Claims.First(c => c.Type == "trialEndsAt").Value);
        Assert.Equal(subscriptionEndsAt.ToString("O"), jwt.Claims.First(c => c.Type == "subscriptionEndsAt").Value);
    }

    [Fact]
    public void CreateToken_WithEntitlement_OmitsNullDateClaims()
    {
        var service = new JwtTokenService(_jwtOptions);
        var entitlement = new HouseholdEntitlement(HouseholdPlan.Free, SubscriptionStatus.None, null);

        var token = service.CreateToken(Guid.NewGuid(), Guid.NewGuid(), "test@mail.com", "Test", entitlement);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "trialEndsAt");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "subscriptionEndsAt");
    }
}
