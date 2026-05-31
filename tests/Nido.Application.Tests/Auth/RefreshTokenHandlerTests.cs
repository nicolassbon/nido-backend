using Nido.Application.Auth;
using Nido.Application.Common.ProfileImages;

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
        var handler = new RefreshTokenHandler(repo, new FakeJwt());

        var result = await handler.Handle(new RefreshTokenCommand("valid-refresh"), CancellationToken.None);

        Assert.Equal("token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var handler = new RefreshTokenHandler(repo, new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None));

        Assert.Equal("INVALID_REFRESH_TOKEN", ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyToken_ThrowsUnauthorized()
    {
        var repo = new FakeAuthRepository();
        var handler = new RefreshTokenHandler(repo, new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand(""), CancellationToken.None));

        Assert.Equal("MISSING_REFRESH_TOKEN", ex.Message);
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
        var handler = new RefreshTokenHandler(repo, new FakeJwt());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None));

        Assert.Equal("INVALID_REFRESH_TOKEN", ex.Message);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public RefreshTokenInfo? TokenInfo { get; set; }
        public User? User { get; set; }
        public Guid? HogarId { get; set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, UserProfileImageMetadata? profileImage, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(User);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(TokenInfo);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(HogarId ?? Guid.NewGuid());
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
