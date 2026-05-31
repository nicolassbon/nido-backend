using Nido.Application.Auth;

namespace Nido.Application.Tests.Auth;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserAndReturnsToken()
    {
        var repo = new FakeAuthRepository();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        var result = await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UsuarioId);
        Assert.NotEqual(Guid.Empty, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("hashed:Password1", repo.StoredHash);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_Throws()
    {
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(Guid.NewGuid(), "Test", "nico@mail.com", "hashed:Old", null, null)
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_AddsPasswordAndReturnsTokens()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(userId, "Test", "nico@mail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        var result = await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.NotNull(repo.LastUpdatedUser);
        Assert.Equal("hashed:Password1", repo.LastUpdatedUser!.PasswordHash);
        Assert.NotNull(repo.StoredRefreshTokenHash);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_PersistsRefreshToken()
    {
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(Guid.NewGuid(), "Test", "nico@mail.com", null, "google", "google-id-1")
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.Equal("hash:refresh", repo.StoredRefreshTokenHash);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? ExistingUser { get; set; }
        public string StoredHash { get; private set; } = string.Empty;
        public Guid? HogarId { get; set; }
        public User? LastUpdatedUser { get; private set; }
        public string? StoredRefreshTokenHash { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(ExistingUser is not null);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(string nombre, string email, string passwordHash, string sexo, string? fotoUrl, CancellationToken cancellationToken, string? oauthProvider = null, string? oauthId = null)
        {
            StoredHash = passwordHash;
            return Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(ExistingUser);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(ExistingUser);

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
        public (string AccessToken, string RefreshToken) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre) => ("token", "refresh");
    }
}
