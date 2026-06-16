using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.Notifications;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class PasswordManagementEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public PasswordManagementEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsSameMessage_ForExistingAndMissingEmail()
    {
        var spy = new SpyEmailService();
        var testFactory = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(spy);
        });
        var client = testFactory.CreateClient();

        var passwordEmail = $"forgot-password-{Guid.NewGuid()}@test.com";
        var googleEmail = $"forgot-google-{Guid.NewGuid()}@test.com";

        using (var scope = testFactory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Pwd", passwordEmail, hasher.Hash("Password123!"), "M", null, true, CancellationToken.None);
            await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google", googleEmail, "google", "google-id"), CancellationToken.None);
        }

        var r1 = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = passwordEmail });
        var r2 = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = googleEmail });
        var r3 = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "missing@test.com" });

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r3.StatusCode);

        var b1 = await r1.Content.ReadFromJsonAsync<MessageBody>();
        var b2 = await r2.Content.ReadFromJsonAsync<MessageBody>();
        var b3 = await r3.Content.ReadFromJsonAsync<MessageBody>();

        Assert.Equal(b1!.Message, b2!.Message);
        Assert.Equal(b1.Message, b3!.Message);
        Assert.Single(spy.PasswordResetEmails);
        Assert.Single(spy.GoogleInfoEmails);
    }

    [Fact]
    public async Task ForgotPassword_WithMalformedJson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        using var content = new StringContent("{ \"email\": ", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/forgot-password", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOkAndAllowsLoginWithNewPassword()
    {
        var client = _factory.CreateClient();
        var email = $"reset-{Guid.NewGuid()}@test.com";
        const string oldPassword = "Password123!";
        const string newPassword = "NewPassword123!";
        string rawToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, _) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Reset User", email, hasher.Hash(oldPassword), "M", null, true, CancellationToken.None);
            rawToken = tokenService.GenerateRefreshToken();
            await repo.SavePasswordResetTokenAsync(userId, tokenService.HashRefreshToken(rawToken), DateTime.UtcNow.AddMinutes(30), CancellationToken.None);
        }

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword,
            newPasswordConfirmation = newPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var oldPasswordLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLoginResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword = "AnotherPassword123!",
            newPasswordConfirmation = "AnotherPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var secondProblem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(secondProblem);
        Assert.Equal(400, secondProblem!.Status);
        Assert.Equal("INVALID_RESET_TOKEN", secondProblem.Title);
        Assert.Equal("Reset token is invalid or expired.", secondProblem.Detail);
    }

    [Fact]
    public async Task ResetPassword_WithBlankToken_ReturnsBadRequestProblemDetails()
    {
        var client = _factory.CreateClient();

        var missingTokenResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "",
            newPassword = "ValidPassword123!",
            newPasswordConfirmation = "ValidPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, missingTokenResponse.StatusCode);

        var problem = await missingTokenResponse.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("INVALID_RESET_TOKEN", problem.Title);
        Assert.Equal("Reset token is invalid or expired.", problem.Detail);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsBadRequestProblemDetails()
    {
        var client = _factory.CreateClient();

        var email = $"reset-expired-{Guid.NewGuid()}@test.com";
        const string oldPassword = "Password123!";
        string expiredRawToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, _) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Expired User", email, hasher.Hash(oldPassword), "M", null, true, CancellationToken.None);
            expiredRawToken = tokenService.GenerateRefreshToken();
            await repo.SavePasswordResetTokenAsync(userId, tokenService.HashRefreshToken(expiredRawToken), DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);
        }

        var expiredTokenResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = expiredRawToken,
            newPassword = "AnotherValid123!",
            newPasswordConfirmation = "AnotherValid123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expiredTokenResponse.StatusCode);

        var problem = await expiredTokenResponse.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("INVALID_RESET_TOKEN", problem.Title);
        Assert.Equal("Reset token is invalid or expired.", problem.Detail);
    }

    [Fact]
    public async Task WeakPasswords_AreRejected_InResetChangeAndAddFlows()
    {
        var client = _factory.CreateClient();
        var email = $"weak-reset-{Guid.NewGuid()}@test.com";
        const string oldPassword = "Password123!";
        string rawToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, _) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Weak Reset", email, hasher.Hash(oldPassword), "M", null, true, CancellationToken.None);
            rawToken = tokenService.GenerateRefreshToken();
            await repo.SavePasswordResetTokenAsync(userId, tokenService.HashRefreshToken(rawToken), DateTime.UtcNow.AddMinutes(30), CancellationToken.None);
        }

        var weakReset = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword = "weak",
            newPasswordConfirmation = "weak"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weakReset.StatusCode);

        var passwordUserClient = _factory.CreateClient();
        string passwordUserToken;
        var passwordEmail = $"weak-change-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Weak Change", passwordEmail, hasher.Hash(oldPassword), "M", null, true, CancellationToken.None);
            passwordUserToken = tokenService.CreateToken(userId, hogarId, passwordEmail, "Weak Change");
        }

        passwordUserClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", passwordUserToken);
        var weakChange = await passwordUserClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = oldPassword,
            newPassword = "weak",
            newPasswordConfirmation = "weak"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weakChange.StatusCode);

        var googleClient = _factory.CreateClient();
        string googleToken;
        var googleEmail = $"weak-add-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Weak Add", googleEmail, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
            googleToken = tokenService.CreateToken(userId, hogarId, googleEmail, "Weak Add");
        }

        googleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", googleToken);
        var weakAdd = await googleClient.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword = "weak",
            newPasswordConfirmation = "weak"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weakAdd.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WhenAuthenticated_ReturnsOkAndPersistsNewPassword()
    {
        var client = _factory.CreateClient();
        var email = $"change-{Guid.NewGuid()}@test.com";
        const string currentPassword = "Password123!";
        const string newPassword = "NewPassword123!";
        string passwordUserToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Change User", email, hasher.Hash(currentPassword), "M", null, true, CancellationToken.None);
            passwordUserToken = tokenService.CreateToken(userId, hogarId, email, "Change User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", passwordUserToken);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword,
            newPasswordConfirmation = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var oldPasswordLoginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = currentPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLoginResponse.StatusCode);

        var newPasswordLoginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newPasswordLoginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Password123!",
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsUnauthorizedProblemDetails()
    {
        var client = _factory.CreateClient();
        var email = $"change-wrong-{Guid.NewGuid()}@test.com";
        const string currentPassword = "Password123!";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Change User", email, hasher.Hash(currentPassword), "M", null, true, CancellationToken.None);
            token = tokenService.CreateToken(userId, hogarId, email, "Change User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong123!",
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("INVALID_CREDENTIALS", problem.Title);
        Assert.Equal("Invalid email or password", problem.Detail);
    }

    [Fact]
    public async Task ChangePassword_WithoutExistingPassword_ReturnsPasswordNotSetProblemDetails()
    {
        var client = _factory.CreateClient();
        var email = $"google-only-{Guid.NewGuid()}@test.com";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google Only", email, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
            token = tokenService.CreateToken(userId, hogarId, email, "Google Only");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "DoesNotMatter123!",
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("PASSWORD_NOT_SET", problem.Title);
        Assert.Equal("This account does not have a password yet.", problem.Detail);
    }

    [Fact]
    public async Task ChangePassword_WithSamePassword_ReturnsPasswordSameAsCurrentProblemDetails()
    {
        var client = _factory.CreateClient();
        var email = $"change-same-{Guid.NewGuid()}@test.com";
        const string currentPassword = "Password123!";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Change User", email, hasher.Hash(currentPassword), "M", null, true, CancellationToken.None);
            token = tokenService.CreateToken(userId, hogarId, email, "Change User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword = currentPassword,
            newPasswordConfirmation = currentPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("PASSWORD_SAME_AS_CURRENT", problem.Title);
        Assert.Equal("New password must be different from the current password.", problem.Detail);
    }

    [Fact]
    public async Task AddPassword_WhenAuthenticatedWithoutPassword_ReturnsOkAndStoresPassword()
    {
        var client = _factory.CreateClient();
        var email = $"add-password-{Guid.NewGuid()}@test.com";
        const string newPassword = "NewPassword123!";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Add Password User", email, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
            token = tokenService.CreateToken(userId, hogarId, email, "Add Password User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword,
            newPasswordConfirmation = newPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task AddPassword_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddPassword_WhenPasswordAlreadyExists_ReturnsConflictProblemDetails()
    {
        var client = _factory.CreateClient();
        var email = $"add-password-existing-{Guid.NewGuid()}@test.com";
        const string currentPassword = "Password123!";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Existing Password User", email, hasher.Hash(currentPassword), "M", null, true, CancellationToken.None);
            token = tokenService.CreateToken(userId, hogarId, email, "Existing Password User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("PASSWORD_ALREADY_SET", problem.Title);
        Assert.Equal("This account already has a password.", problem.Detail);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = currentPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private sealed record MessageBody(string Message);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
    private sealed class SpyEmailService : IEmailService
    {
        public List<string> InvitationEmails { get; } = [];
        public List<string> PasswordResetEmails { get; } = [];
        public List<string> GoogleInfoEmails { get; } = [];
        public List<string> DuplicateSignupNoticeEmails { get; } = [];

        public Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct)
        {
            InvitationEmails.Add(toEmail);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct)
        {
            PasswordResetEmails.Add(toEmail);
            return Task.CompletedTask;
        }

        public Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct)
        {
            GoogleInfoEmails.Add(toEmail);
            return Task.CompletedTask;
        }

        public Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct)
        {
            DuplicateSignupNoticeEmails.Add(toEmail);
            return Task.CompletedTask;
        }
    }
}
