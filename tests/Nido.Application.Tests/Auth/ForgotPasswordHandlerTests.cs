using Nido.Application.Auth;
using Nido.Application.Auth.ForgotPassword;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Common.Notifications;
using Nido.Application.Payments;

namespace Nido.Application.Tests.Auth;

public sealed class ForgotPasswordHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingLocalUser_GeneratesResetTokenAndSendsEmail()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            User = new User(userId, "Local User", "local-user@test.com", "hashed:Password123!", null, null)
        };
        var emailService = new SpyEmailService();
        var handler = new ForgotPasswordHandler(repository, new FakeJwtTokenService(), emailService);

        var result = await handler.Handle(
            new ForgotPasswordCommand("local-user@test.com"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, repository.SavedResetTokenUserId);
        Assert.Equal("hash:reset-token", repository.SavedResetTokenHash);
        Assert.NotNull(repository.SavedResetTokenExpiresAt);
        Assert.Contains("local-user@test.com", emailService.PasswordResetEmails);
        Assert.Equal("reset-token", emailService.LastResetToken);
        Assert.Empty(emailService.GoogleOnlyInfoEmails);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsSilentSuccess()
    {
        var repository = new FakeAuthRepository();
        var emailService = new SpyEmailService();
        var handler = new ForgotPasswordHandler(repository, new FakeJwtTokenService(), emailService);

        var result = await handler.Handle(
            new ForgotPasswordCommand("missing@test.com"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(repository.SavedResetTokenUserId);
        Assert.Empty(emailService.PasswordResetEmails);
        Assert.Empty(emailService.GoogleOnlyInfoEmails);
    }

    [Fact]
    public async Task Handle_WithBlankEmail_ReturnsSilentSuccess()
    {
        var repository = new FakeAuthRepository();
        var emailService = new SpyEmailService();
        var handler = new ForgotPasswordHandler(repository, new FakeJwtTokenService(), emailService);

        var result = await handler.Handle(
            new ForgotPasswordCommand("   "),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(repository.LastRequestedEmail);
        Assert.Null(repository.SavedResetTokenUserId);
        Assert.Empty(emailService.PasswordResetEmails);
        Assert.Empty(emailService.GoogleOnlyInfoEmails);
    }

    [Fact]
    public async Task Handle_WithGoogleOnlyUser_SendsProviderSpecificInstructions()
    {
        var repository = new FakeAuthRepository
        {
            User = new User(Guid.NewGuid(), "Google User", "google-only@test.com", null, "google", "google-id")
        };
        var emailService = new SpyEmailService();
        var handler = new ForgotPasswordHandler(repository, new FakeJwtTokenService(), emailService);

        var result = await handler.Handle(
            new ForgotPasswordCommand("google-only@test.com"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(repository.SavedResetTokenUserId);
        Assert.Empty(emailService.PasswordResetEmails);
        Assert.Contains("google-only@test.com", emailService.GoogleOnlyInfoEmails);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? User { get; set; }
        public string? LastRequestedEmail { get; private set; }
        public Guid? SavedResetTokenUserId { get; private set; }
        public string? SavedResetTokenHash { get; private set; }
        public DateTime? SavedResetTokenExpiresAt { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, string? fotoStorageKey, bool aceptaTerminos, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            LastRequestedEmail = email;
            return Task.FromResult(User);
        }

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
        {
            SavedResetTokenUserId = usuarioId;
            SavedResetTokenHash = tokenHash;
            SavedResetTokenExpiresAt = expiresAt;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<PasswordResetTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan) => CreateToken(usuarioId, hogarId, email, nombre);
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement) => CreateToken(usuarioId, hogarId, email, nombre);

        public string GenerateRefreshToken() => "reset-token";

        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";

        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("token", "refresh", DateTime.UtcNow.AddDays(7));
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
    }

    private sealed class SpyEmailService : IEmailService
    {
        public List<string> InvitationEmails { get; } = [];
        public List<string> PasswordResetEmails { get; } = [];
        public List<string> GoogleOnlyInfoEmails { get; } = [];
        public List<string> DuplicateSignupNoticeEmails { get; } = [];
        public string? LastResetToken { get; private set; }

        public Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct)
            => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct)
        {
            PasswordResetEmails.Add(toEmail);
            LastResetToken = resetToken;
            return Task.CompletedTask;
        }

        public Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct)
        {
            GoogleOnlyInfoEmails.Add(toEmail);
            return Task.CompletedTask;
        }

        public Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct)
            => Task.CompletedTask;
    }
}
