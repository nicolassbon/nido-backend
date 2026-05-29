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
        var repo = new FakeAuthRepository { Existing = true };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None));
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public bool Existing { get; set; }
        public string StoredHash { get; private set; } = string.Empty;

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Existing);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(string nombre, string email, string passwordHash, string sexo, string? fotoUrl, CancellationToken cancellationToken)
        {
            StoredHash = passwordHash;
            return Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
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
