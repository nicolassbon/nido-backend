using Nido.Application.Auth.Google.Link;
using Nido.Application.Auth.Google.Login;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Tests.Auth;

public sealed class LinkGoogleHandlerTests
{
    [Fact]
    public async Task Handle_Success_LinksGoogleAndReturnsToken()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null),
            HogarId = hogarId
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("nico@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var result = await handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None);

        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("google", repo.UpdatedUserOauthProvider);
        Assert.Equal("google-id-123", repo.UpdatedUserOauthId);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFound()
    {
        var repo = new FakeAuthRepository();
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("nico@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new LinkGoogleCommand(Guid.NewGuid(), "valid-token"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyLinked_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", "google", "google-id-123")
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("nico@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<AccountAlreadyLinkedException>(() =>
            handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None));

        Assert.Equal("ACCOUNT_ALREADY_LINKED", ex.Code);
    }

    [Fact]
    public async Task Handle_GoogleEmailDoesNotMatchUserEmail_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("other@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<InvalidGoogleTokenException>(() =>
            handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None));

        Assert.Equal("GOOGLE_EMAIL_MISMATCH", ex.Code);
        Assert.Equal("Google email mismatch.", ex.Message);
    }

    [Fact]
    public async Task Handle_GoogleIdLinkedToDifferentUser_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null),
            GoogleUser = new User(Guid.NewGuid(), "Test", "other@mail.com", null, "google", "google-id-123")
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("nico@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<AccountAlreadyLinkedException>(() =>
            handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None));

        Assert.Equal("GOOGLE_ACCOUNT_ALREADY_LINKED", ex.Code);
    }

    [Fact]
    public async Task Handle_GoogleEmailMatchesAfterNormalization_LinksGoogle()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "NICO@MAIL.COM", "hashed:Password1", null, null),
            HogarId = Guid.NewGuid()
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload(" nico@mail.com ", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var result = await handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None);

        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal("google-id-123", repo.UpdatedUserOauthId);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var validator = new FakeGoogleValidator { ThrowInvalid = true };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<InvalidGoogleTokenException>(() =>
            handler.Handle(new LinkGoogleCommand(Guid.NewGuid(), "invalid-token"), CancellationToken.None));

        Assert.Equal("INVALID_GOOGLE_TOKEN", ex.Code);
        Assert.Equal("Invalid Google token.", ex.Message);
    }

    [Fact]
    public async Task Handle_UserHasNoHousehold_ThrowsNoHouseholdAssociatedException()
    {
        var userId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null),
            HogarId = null
        };
        var validator = new FakeGoogleValidator { Payload = new GooglePayload("nico@mail.com", "google-id-123") };
        var handler = new LinkGoogleHandler(repo, validator, new FakeJwt());

        var ex = await Assert.ThrowsAsync<NoHouseholdAssociatedException>(() =>
            handler.Handle(new LinkGoogleCommand(userId, "valid-token"), CancellationToken.None));

        Assert.Equal("NO_HOUSEHOLD_ASSOCIATED", ex.Code);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? User { get; set; }
        public User? GoogleUser { get; set; }
        public string? StoredRefreshTokenHash { get; private set; }
        public string? UpdatedUserOauthProvider { get; private set; }
        public string? UpdatedUserOauthId { get; private set; }
        public bool Existing { get; set; }
        public Guid? HogarId { get; set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Existing);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, UserProfileImageMetadata? profileImage, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult(GoogleUser);

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
            UpdatedUserOauthProvider = user.OauthProvider;
            UpdatedUserOauthId = user.OauthId;
            return Task.CompletedTask;
        }

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(HogarId);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(User);
    }

    private sealed class FakeGoogleValidator : IGoogleTokenValidator
    {
        public GooglePayload? Payload { get; set; }
        public bool ThrowInvalid { get; set; }

        public Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            if (ThrowInvalid)
                throw new InvalidOperationException("Invalid token");
            return Task.FromResult(Payload!);
        }
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("token", "refresh", DateTime.UtcNow.AddDays(7));
    }
}
