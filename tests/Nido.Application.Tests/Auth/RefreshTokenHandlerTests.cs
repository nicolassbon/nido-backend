using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Payments;

namespace Nido.Application.Tests.Auth;

public sealed class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewAccessToken()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenHash = "hash:valid-refresh";
        var repo = new FakeAuthRepository
        {
            TokenInfo = new RefreshTokenInfo(Guid.NewGuid(), userId, tokenHash, DateTime.UtcNow.AddDays(7)),
            HogarId = hogarId,
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var handler = new RefreshTokenHandler(repo, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var result = await handler.Handle(new RefreshTokenCommand("valid-refresh"), CancellationToken.None);

        Assert.Equal("token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var handler = new RefreshTokenHandler(repo, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            handler.Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None));

        Assert.Equal("INVALID_REFRESH_TOKEN", ex.Code);
        Assert.Equal("Invalid refresh token.", ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyToken_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var handler = new RefreshTokenHandler(repo, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            handler.Handle(new RefreshTokenCommand(""), CancellationToken.None));

        Assert.Equal("MISSING_REFRESH_TOKEN", ex.Code);
        Assert.Equal("Invalid refresh token.", ex.Message);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            // GetValidRefreshTokenAsync returns null for expired tokens (already filtered in repo)
            TokenInfo = null,
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var handler = new RefreshTokenHandler(repo, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None));

        Assert.Equal("INVALID_REFRESH_TOKEN", ex.Code);
        Assert.Equal("Invalid refresh token.", ex.Message);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_LooksUpCurrentPlanAndPassesToToken()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenHash = "hash:valid-refresh";
        var repo = new FakeAuthRepository
        {
            TokenInfo = new RefreshTokenInfo(Guid.NewGuid(), userId, tokenHash, DateTime.UtcNow.AddDays(7)),
            HogarId = hogarId,
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var entitlementService = new FakeEntitlementService(HouseholdPlan.Premium, SubscriptionStatus.Active, null);
        var jwt = new FakeJwt();
        var handler = new RefreshTokenHandler(repo, jwt, entitlementService);

        var result = await handler.Handle(new RefreshTokenCommand("valid-refresh"), CancellationToken.None);

        Assert.Equal("token", result.AccessToken);
        Assert.Equal(HouseholdPlan.Premium, jwt.LastCreateTokenEntitlement?.Plan);
        Assert.Equal(hogarId, entitlementService.LastRequestedHogarId);
        Assert.Equal(HouseholdPlan.Premium, result.Plan);
        Assert.Equal(SubscriptionStatus.Active, result.SubscriptionStatus);
        Assert.Null(result.TrialEndsAt);
    }

    [Fact]
    public async Task Handle_PlanChangedToFree_PassesFreePlanToToken()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            TokenInfo = new RefreshTokenInfo(Guid.NewGuid(), userId, "hash:valid-refresh", DateTime.UtcNow.AddDays(7)),
            HogarId = hogarId,
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var entitlementService = new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null);
        var jwt = new FakeJwt();
        var handler = new RefreshTokenHandler(repo, jwt, entitlementService);

        var result = await handler.Handle(new RefreshTokenCommand("valid-refresh"), CancellationToken.None);

        Assert.Equal(HouseholdPlan.Free, jwt.LastCreateTokenEntitlement?.Plan);
        Assert.Equal(HouseholdPlan.Free, result.Plan);
        Assert.Equal(SubscriptionStatus.None, result.SubscriptionStatus);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public RefreshTokenInfo? TokenInfo { get; set; }
        public User? User { get; set; }
        public Guid? HogarId { get; set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, bool aceptaTerminos, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(TokenInfo);

        public Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<PasswordResetTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(HogarId ?? Guid.NewGuid());
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public HouseholdEntitlement? LastCreateTokenEntitlement { get; private set; }

        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan) => "token";
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
        {
            LastCreateTokenEntitlement = entitlement;
            return "token";
        }

        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("token", "refresh", DateTime.UtcNow.AddDays(7));
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
    }

    private sealed class FakeEntitlementService : IEntitlementService
    {
        private readonly HouseholdEntitlement _entitlement;

        public FakeEntitlementService(HouseholdPlan plan, SubscriptionStatus status, DateTime? trialEndsAt)
        {
            _entitlement = new HouseholdEntitlement(plan, status, trialEndsAt);
        }

        public Guid LastRequestedHogarId { get; private set; }

        public Task EnsurePremiumAsync(Guid hogarId, CancellationToken ct)
        {
            if (_entitlement.Plan != HouseholdPlan.Premium && (!_entitlement.TrialEndsAt.HasValue || _entitlement.TrialEndsAt.Value <= DateTime.UtcNow))
            {
                throw new InvalidOperationException("Premium required");
            }
            return Task.CompletedTask;
        }

        public Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct)
        {
            LastRequestedHogarId = hogarId;
            return Task.FromResult(_entitlement);
        }
    }
}
