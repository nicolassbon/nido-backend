using Nido.Application.Auth;

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
            User = new User(userId, "nico@mail.com", "hashed:Password1", null, null),
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
            User = new User(Guid.NewGuid(), "nico@mail.com", "hashed:Password1", null, null)
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "WrongPassword"), CancellationToken.None));

        Assert.Equal("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "nico@mail.com", null, "google", "google-id-123")
        };
        var handler = new LoginHandler(repo, new FakeHasher(), new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("nico@mail.com", "Password1"), CancellationToken.None));

        Assert.Equal("Invalid email or password", ex.Message);
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

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(string nombre, string email, string passwordHash, string sexo, string? fotoUrl, CancellationToken cancellationToken, string? oauthProvider = null, string? oauthId = null)
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
        public string CreateToken(Guid usuarioId, Guid hogarId, string email) => "token";
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email) => ("token", "refresh");
    }
}
