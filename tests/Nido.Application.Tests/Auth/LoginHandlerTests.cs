using Nido.Application.Auth;
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
    public async Task Handle_UserNotFound_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("missing@mail.com", "Password1"), CancellationToken.None));

        Assert.Equal("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Test", "nico@mail.com", "hashed:Password1", null, null)
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "WrongPassword"), CancellationToken.None));

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
    public async Task Handle_EmptyEmail_ThrowsArgumentException()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new LoginCommand("", "Password1"), CancellationToken.None));

        Assert.Equal("Email and password are required.", ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyPassword_ThrowsArgumentException()
    {
        var repo = new FakeAuthRepository();
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", ""), CancellationToken.None));

        Assert.Equal("Email and password are required.", ex.Message);
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

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, UserProfileImageMetadata? profileImage, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
        {
            StoredRefreshTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(HogarId ?? Guid.NewGuid());

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
