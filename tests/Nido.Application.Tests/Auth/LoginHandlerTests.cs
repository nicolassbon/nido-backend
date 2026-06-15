using Nido.Application.Auth.Login;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Tests.Auth;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            User = new User(userId, "Test", "nico@mail.com", "hashed:Password1", null, null),
            HogarId = hogarId
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var result = await handler.Handle(new LoginCommand("nico@mail.com", "Password1"), CancellationToken.None);

        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("hash:refresh", repo.StoredRefreshTokenHash);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidCredentials()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new LoginCommand("missing@mail.com", "Password1"), CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
        Assert.Equal("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentials()
    {
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "WrongPassword"), CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
        Assert.Equal("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_ThrowsAccountLinkRequired()
    {
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "nico@mail.com", null, "google", "google-id-123")
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<AccountLinkRequiredException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "Password1"), CancellationToken.None));

        Assert.Equal("ACCOUNT_EXISTS_WITH_GOOGLE", ex.Code);
        Assert.Equal("This account was created with Google. Use Google login or set a password from account linking.", ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyEmail_ThrowsLoginCredentialsMissing()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<LoginCredentialsMissingException>(() =>
            handler.Handle(new LoginCommand("", "Password1"), CancellationToken.None));

        Assert.Equal("LOGIN_CREDENTIALS_MISSING", ex.Code);
        Assert.Equal("Email and password are required.", ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyPassword_ThrowsLoginCredentialsMissing()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<LoginCredentialsMissingException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", ""), CancellationToken.None));

        Assert.Equal("LOGIN_CREDENTIALS_MISSING", ex.Code);
        Assert.Equal("Email and password are required.", ex.Message);
    }

    [Fact]
    public async Task Handle_NoHousehold_ThrowsUserNotInHousehold()
    {
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "nico@mail.com", "hashed:Password1", null, null),
            HogarId = null
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<UserNotInHouseholdException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "Password1"), CancellationToken.None));

        Assert.Equal("USER_NOT_IN_HOUSEHOLD", ex.Code);
        Assert.Equal("User has no associated household.", ex.Message);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? User { get; set; }
        public string? StoredRefreshTokenHash { get; private set; }
        public bool Existing { get; set; }
        public Guid? HogarId { get; set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Existing);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

<<<<<<< HEAD
        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, bool aceptaTerminos, CancellationToken cancellationToken)
=======
        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, CancellationToken cancellationToken)
>>>>>>> fd30f2e558fc3a1ca16002a6876d92ea848c6f84
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

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

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult(HogarId);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
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
