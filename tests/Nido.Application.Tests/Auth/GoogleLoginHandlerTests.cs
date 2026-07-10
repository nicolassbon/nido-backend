using Nido.Application.Auth.Google.Login;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Payments;

namespace Nido.Application.Tests.Auth;

public sealed class GoogleLoginHandlerTests
{
    [Fact]
    public async Task Handle_NewUser_CreatesUserAndReturnsTokens()
    {
        var googleValidator = new FakeGoogleValidator("new@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.NotNull(repo.StoredRefreshTokenHash);
        Assert.Equal("new@gmail.com", repo.CreatedEmail);
    }

    [Fact]
    public async Task Handle_NewUser_ReturnsFreePlan()
    {
        var googleValidator = new FakeGoogleValidator("new@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.Equal(HouseholdPlan.Free, result.Plan);
        Assert.Equal(SubscriptionStatus.None, result.SubscriptionStatus);
        Assert.Null(result.TrialEndsAt);
    }

    [Fact]
    public async Task Handle_ExistingGoogleUser_ReturnsTokensNotNew()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var googleValidator = new FakeGoogleValidator("existing@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "existing@gmail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var entitlementService = new FakeEntitlementService(HouseholdPlan.Premium, SubscriptionStatus.Active, null);
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), entitlementService);

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal(HouseholdPlan.Premium, result.Plan);
        Assert.Equal(hogarId, entitlementService.LastRequestedHogarId);
    }

    [Fact]
    public async Task Handle_GoogleIdMatchWithChangedEmail_ReturnsLinkedUserTokens()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var googleValidator = new FakeGoogleValidator("new-email@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            GoogleUser = new User(userId, "Test", "old-email@gmail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(userId, result.UsuarioId);
    }

    [Fact]
    public async Task Handle_EmailMatchesDifferentLinkedGoogleId_ThrowsUnauthorized()
    {
        var googleValidator = new FakeGoogleValidator("existing@gmail.com", "google-id-new");
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "existing@gmail.com", null, "google", "google-id-existing")
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<InvalidGoogleTokenException>(() =>
            handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None));

        Assert.Equal("GOOGLE_ACCOUNT_MISMATCH", ex.Code);
        Assert.Equal("Google account mismatch.", ex.Message);
    }

    [Fact]
    public async Task Handle_PasswordOnlyUser_ThrowsAccountLinkRequired()
    {
        var googleValidator = new FakeGoogleValidator("user@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "user@gmail.com", "hashed:Password1", null, null)
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<AccountLinkRequiredException>(() =>
            handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None));

        Assert.Equal("ACCOUNT_EXISTS_WITH_PASSWORD", ex.Code);
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_ThrowsUnauthorized()
    {
        var googleValidator = new FakeGoogleValidator(throws: true);
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        var ex = await Assert.ThrowsAsync<InvalidGoogleTokenException>(() =>
            handler.Handle(new GoogleLoginCommand("invalid-token"), CancellationToken.None));

        Assert.Equal("INVALID_GOOGLE_TOKEN", ex.Code);
        Assert.Equal("Invalid Google token.", ex.Message);
    }

    [Fact]
    public async Task Handle_NewUser_WithHttpPicture_DoesNotPersistExternalImage()
    {
        var googleValidator = new FakeGoogleValidator("new@gmail.com", "google-id-1", "http://lh3.googleusercontent.com/a/insecure");
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt(), new FakeEntitlementService(HouseholdPlan.Free, SubscriptionStatus.None, null));

        await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.Null(repo.CreatedFotoStorageKey);
    }

    private sealed class FakeGoogleValidator : IGoogleTokenValidator
    {
        private readonly string? _email;
        private readonly string? _googleId;
        private readonly string? _picture;
        private readonly bool _throws;

        public FakeGoogleValidator(string? email = null, string? googleId = null, string? picture = null, bool throws = false)
        {
            _email = email;
            _googleId = googleId;
            _picture = picture;
            _throws = throws;
        }

        public Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            if (_throws) throw new Exception("Invalid token");
            return Task.FromResult(new GooglePayload(_email!, _googleId!, Picture: _picture));
        }
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? User { get; set; }
        public User? GoogleUser { get; set; }
        public string? StoredRefreshTokenHash { get; private set; }
        public Guid? HogarId { get; set; }
        public string? CreatedEmail { get; private set; }
        public string? CreatedFotoStorageKey { get; private set; }
        public User? LastUpdatedUser { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User is not null);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
        {
            CreatedEmail = data.Email;
            CreatedFotoStorageKey = data.FotoStorageKey;
            var id = Guid.NewGuid();
            var hid = Guid.NewGuid();
            return Task.FromResult((id, hid));
        }

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, bool aceptaTerminos, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult(GoogleUser);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
        {
            StoredRefreshTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<PasswordResetTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken)
        {
            LastUpdatedUser = user;
            return Task.CompletedTask;
        }

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(HogarId ?? Guid.NewGuid());
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan) => CreateToken(usuarioId, hogarId, email, nombre);
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement) => CreateToken(usuarioId, hogarId, email, nombre);
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

        public Task EnsurePremiumAsync(Guid hogarId, CancellationToken ct) => Task.CompletedTask;

        public Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct)
        {
            LastRequestedHogarId = hogarId;
            return Task.FromResult(_entitlement);
        }
    }
}
