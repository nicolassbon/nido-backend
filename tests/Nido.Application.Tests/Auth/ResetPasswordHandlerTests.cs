using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth;

namespace Nido.Application.Tests.Auth;

public sealed class ResetPasswordHandlerTests
{
    [Fact]
    public async Task Handle_WithValidToken_UpdatesPasswordAndConsumesToken()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            PasswordResetToken = new PasswordResetTokenInfo(Guid.NewGuid(), userId, "hash:reset-token", DateTime.UtcNow.AddMinutes(30), null)
        };
        var handler = new ResetPasswordHandler(repository, new FakeJwtTokenService(), new FakePasswordHasher());

        var result = await handler.Handle(
            new ResetPasswordCommand("reset-token", "NewPassword123!", "NewPassword123!"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, repository.UpdatedUserId);
        Assert.Equal("hashed:NewPassword123!", repository.UpdatedPasswordHash);
        Assert.Equal("hash:reset-token", repository.ConsumedTokenHash);
    }

    [Fact]
    public async Task Handle_WithBlankToken_ThrowsInvalidResetToken()
    {
        var repository = new FakeAuthRepository();
        var handler = new ResetPasswordHandler(repository, new FakeJwtTokenService(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidResetTokenException>(() =>
            handler.Handle(new ResetPasswordCommand("", "NewPassword123!", "NewPassword123!"), CancellationToken.None));

        Assert.Equal("INVALID_RESET_TOKEN", exception.Code);
        Assert.Equal("Reset token is invalid or expired.", exception.Message);
        Assert.Null(repository.LastRequestedTokenHash);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsInvalidResetToken()
    {
        var repository = new FakeAuthRepository();
        var handler = new ResetPasswordHandler(repository, new FakeJwtTokenService(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidResetTokenException>(() =>
            handler.Handle(new ResetPasswordCommand("unknown-token", "NewPassword123!", "NewPassword123!"), CancellationToken.None));

        Assert.Equal("INVALID_RESET_TOKEN", exception.Code);
        Assert.Equal("Reset token is invalid or expired.", exception.Message);
        Assert.Equal("hash:unknown-token", repository.LastRequestedTokenHash);
        Assert.Null(repository.UpdatedPasswordHash);
        Assert.Null(repository.ConsumedTokenHash);
    }

    [Fact]
    public async Task Handle_WithPasswordConfirmationMismatch_ThrowsValidationError()
    {
        var repository = new FakeAuthRepository();
        var handler = new ResetPasswordHandler(repository, new FakeJwtTokenService(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidPasswordException>(() =>
            handler.Handle(new ResetPasswordCommand("reset-token", "NewPassword123!", "DifferentPassword123!"), CancellationToken.None));

        Assert.Equal("PASSWORD_CONFIRMATION_MISMATCH", exception.Code);
        Assert.Equal("Password confirmation does not match.", exception.Message);
        Assert.Null(repository.LastRequestedTokenHash);
    }

    [Fact]
    public async Task Handle_WithWeakPassword_ThrowsWeakPassword()
    {
        var repository = new FakeAuthRepository();
        var handler = new ResetPasswordHandler(repository, new FakeJwtTokenService(), new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<WeakPasswordException>(() =>
            handler.Handle(new ResetPasswordCommand("reset-token", "weak", "weak"), CancellationToken.None));

        Assert.Equal("WEAK_PASSWORD", exception.Code);
        Assert.Equal("Password does not meet complexity requirements.", exception.Message);
        Assert.Null(repository.LastRequestedTokenHash);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public PasswordResetTokenInfo? PasswordResetToken { get; set; }
        public string? LastRequestedTokenHash { get; private set; }
        public Guid? UpdatedUserId { get; private set; }
        public string? UpdatedPasswordHash { get; private set; }
        public string? ConsumedTokenHash { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, bool aceptaTerminos, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            LastRequestedTokenHash = tokenHash;
            return Task.FromResult(PasswordResetToken);
        }

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            ConsumedTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken)
        {
            UpdatedUserId = usuarioId;
            UpdatedPasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";

        public string GenerateRefreshToken() => "refresh";

        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";

        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("token", "refresh", DateTime.UtcNow.AddDays(7));
    }
}
