using Nido.Application.Auth.Logout;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Tests.Auth;

public sealed class LogoutHandlerTests
{
    [Fact]
    public async Task Handle_ExistingToken_RemovesFromDatabase()
    {
        var repo = new FakeAuthRepository();
        var handler = new LogoutHandler(repo, new FakeJwt());

        await handler.Handle(new LogoutCommand("valid-refresh"), CancellationToken.None);

        Assert.Equal("hash:valid-refresh", repo.RemovedTokenHash);
    }

    [Fact]
    public async Task Handle_EmptyToken_DoesNotThrow()
    {
        var repo = new FakeAuthRepository();
        var handler = new LogoutHandler(repo, new FakeJwt());

        await handler.Handle(new LogoutCommand(""), CancellationToken.None);

        Assert.Null(repo.RemovedTokenHash);
    }

    [Fact]
    public async Task Handle_NonExistentToken_CompletesSuccessfully()
    {
        var repo = new FakeAuthRepository();
        var handler = new LogoutHandler(repo, new FakeJwt());

        await handler.Handle(new LogoutCommand("non-existent"), CancellationToken.None);

        Assert.Equal("hash:non-existent", repo.RemovedTokenHash);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public string? RemovedTokenHash { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, UserProfileImageMetadata? profileImage, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            RemovedTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(Guid.NewGuid());
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
