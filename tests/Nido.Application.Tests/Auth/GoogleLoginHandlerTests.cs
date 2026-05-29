using Nido.Application.Auth;

namespace Nido.Application.Tests.Auth;

public sealed class GoogleLoginHandlerTests
{
    [Fact]
    public async Task Handle_NewUser_CreatesUserAndReturnsTokens()
    {
        var googleValidator = new FakeGoogleValidator("new@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.NotNull(repo.StoredRefreshTokenHash);
        Assert.Equal("new@gmail.com", repo.CreatedEmail);
    }

    [Fact]
    public async Task Handle_ExistingGoogleUser_ReturnsTokensNotNew()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var googleValidator = new FakeGoogleValidator("existing@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "existing@gmail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal("token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_GoogleIdMatchWithChangedEmail_ReturnsLinkedUserTokens()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var googleValidator = new FakeGoogleValidator("new-email@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            GoogleUser = new User(userId, "old-email@gmail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

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
            User = new User(Guid.NewGuid(), "existing@gmail.com", null, "google", "google-id-existing")
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None));

        Assert.Equal("GOOGLE_ACCOUNT_MISMATCH", ex.Message);
    }

    [Fact]
    public async Task Handle_PasswordOnlyUser_ThrowsAccountLinkRequired()
    {
        var googleValidator = new FakeGoogleValidator("user@gmail.com", "google-id-1");
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "user@gmail.com", "hashed:Password1", null, null)
        };
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<AccountLinkRequiredException>(() =>
            handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None));

        Assert.Equal("ACCOUNT_EXISTS_WITH_PASSWORD", ex.Code);
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_ThrowsUnauthorized()
    {
        var googleValidator = new FakeGoogleValidator(throws: true);
        var repo = new FakeAuthRepository();
        var handler = new GoogleLoginHandler(repo, googleValidator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new GoogleLoginCommand("invalid-token"), CancellationToken.None));

        Assert.Equal("INVALID_GOOGLE_TOKEN", ex.Message);
    }

    private sealed class FakeGoogleValidator : IGoogleTokenValidator
    {
        private readonly string? _email;
        private readonly string? _googleId;
        private readonly bool _throws;

        public FakeGoogleValidator(string? email = null, string? googleId = null, bool throws = false)
        {
            _email = email;
            _googleId = googleId;
            _throws = throws;
        }

        public Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            if (_throws) throw new Exception("Invalid token");
            return Task.FromResult(new GooglePayload(_email!, _googleId!));
        }
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? User { get; set; }
        public User? GoogleUser { get; set; }
        public string? StoredRefreshTokenHash { get; private set; }
        public Guid? HogarId { get; set; }
        public string? CreatedEmail { get; private set; }
        public User? LastUpdatedUser { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User is not null);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(string nombre, string email, string passwordHash, string sexo, string? fotoUrl, CancellationToken cancellationToken, string? oauthProvider = null, string? oauthId = null)
        {
            CreatedEmail = email;
            var id = Guid.NewGuid();
            var hid = Guid.NewGuid();
            return Task.FromResult((id, hid));
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult(GoogleUser);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
        {
            StoredRefreshTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

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
        public string CreateToken(Guid usuarioId, Guid hogarId, string email) => "token";
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email) => ("token", "refresh");
    }
}
