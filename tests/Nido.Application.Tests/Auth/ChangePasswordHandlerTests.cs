using Nido.Application.Auth;
using Nido.Application.Auth.ChangePassword;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;

namespace Nido.Application.Tests.Auth;

public sealed class ChangePasswordHandlerTests
{
    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_UpdatesPassword()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            User = new User(userId, "Change User", "change@test.com", "hashed:CurrentPassword123!", null, null)
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var result = await handler.Handle(
            new ChangePasswordCommand(userId, "CurrentPassword123!", "NewPassword123!", "NewPassword123!"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, repository.UpdatedUserId);
        Assert.Equal("hashed:NewPassword123!", repository.UpdatedPasswordHash);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsInvalidCredentials()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Change User", "change@test.com", "hashed:CurrentPassword123!", null, null)
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(
                new ChangePasswordCommand(repository.User.Id, "WrongPassword123!", "NewPassword123!", "NewPassword123!"),
                CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", exception.Code);
        Assert.Equal("Invalid email or password", exception.Message);
        Assert.Null(repository.UpdatedPasswordHash);
    }

    [Fact]
    public async Task Handle_WithoutExistingPassword_ThrowsPasswordNotSet()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Google User", "google@test.com", null, "google", "google-id")
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidPasswordException>(() =>
            handler.Handle(
                new ChangePasswordCommand(repository.User.Id, "Anything123!", "NewPassword123!", "NewPassword123!"),
                CancellationToken.None));

        Assert.Equal("PASSWORD_NOT_SET", exception.Code);
        Assert.Equal("This account does not have a password yet.", exception.Message);
        Assert.Null(repository.UpdatedPasswordHash);
    }

    [Fact]
    public async Task Handle_WithConfirmationMismatch_ThrowsValidationError()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Change User", "change@test.com", "hashed:CurrentPassword123!", null, null)
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidPasswordException>(() =>
            handler.Handle(
                new ChangePasswordCommand(repository.User.Id, "CurrentPassword123!", "NewPassword123!", "DifferentPassword123!"),
                CancellationToken.None));

        Assert.Equal("PASSWORD_CONFIRMATION_MISMATCH", exception.Code);
        Assert.Equal("Password confirmation does not match.", exception.Message);
        Assert.Null(repository.UpdatedPasswordHash);
    }

    [Fact]
    public async Task Handle_WithSamePassword_ThrowsPasswordSameAsCurrent()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Change User", "change@test.com", "hashed:CurrentPassword123!", null, null)
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidPasswordException>(() =>
            handler.Handle(
                new ChangePasswordCommand(repository.User.Id, "CurrentPassword123!", "CurrentPassword123!", "CurrentPassword123!"),
                CancellationToken.None));

        Assert.Equal("PASSWORD_SAME_AS_CURRENT", exception.Code);
        Assert.Equal("New password must be different from the current password.", exception.Message);
        Assert.Null(repository.UpdatedPasswordHash);
    }

    [Fact]
    public async Task Handle_WithWeakPassword_ThrowsWeakPassword()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Change User", "change@test.com", "hashed:CurrentPassword123!", null, null)
        };
        var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<WeakPasswordException>(() =>
            handler.Handle(
                new ChangePasswordCommand(repository.User.Id, "CurrentPassword123!", "weak", "weak"),
                CancellationToken.None));

        Assert.Equal("WEAK_PASSWORD", exception.Code);
        Assert.Equal("Password does not meet complexity requirements.", exception.Message);
        Assert.Null(repository.UpdatedPasswordHash);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User User { get; set; } = null!;
        public Guid? UpdatedUserId { get; private set; }
        public string? UpdatedPasswordHash { get; private set; }

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

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<PasswordResetTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken)
        {
            UpdatedUserId = usuarioId;
            UpdatedPasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == User.Id ? User : null);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }
}
