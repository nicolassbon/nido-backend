using System.Net;
using System.Net.Http.Headers;
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
    public async Task ResetPassword_ValidToken_UpdatesPasswordAndConsumesToken()
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

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword = "AnotherPassword123!",
            newPasswordConfirmation = "AnotherPassword123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MissingOrExpiredToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var missingTokenResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "",
            newPassword = "ValidPassword123!",
            newPasswordConfirmation = "ValidPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, missingTokenResponse.StatusCode);

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
    public async Task ChangePassword_RequiresCurrentPassword_And_AddPassword_ForGoogleOnly()
    {
        var client = _factory.CreateClient();
        var registerEmail = $"change-{Guid.NewGuid()}@test.com";
        const string currentPassword = "Password123!";
        string passwordUserToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Change User", registerEmail, hasher.Hash(currentPassword), "M", null, true, CancellationToken.None);
            passwordUserToken = tokenService.CreateToken(userId, hogarId, registerEmail, "Change User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", passwordUserToken);

        var wrongCurrent = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong123!",
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongCurrent.StatusCode);

        var sameAsCurrent = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword = currentPassword,
            newPasswordConfirmation = currentPassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, sameAsCurrent.StatusCode);

        var okCurrent = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword = "NewPassword123!",
            newPasswordConfirmation = "NewPassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, okCurrent.StatusCode);

        var googleClient = _factory.CreateClient();
        string googleToken;
        var googleEmail = $"google-only-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google Only", googleEmail, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
            googleToken = tokenService.CreateToken(userId, hogarId, googleEmail, "Google Only");
        }

        googleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", googleToken);
        var addPassword = await googleClient.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword = "GooglePassword123!",
            newPasswordConfirmation = "GooglePassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, addPassword.StatusCode);

        var addAgain = await googleClient.PostAsJsonAsync("/api/auth/add-password", new
        {
            newPassword = "AnotherPassword123!",
            newPasswordConfirmation = "AnotherPassword123!"
        });
        Assert.Equal(HttpStatusCode.Conflict, addAgain.StatusCode);

        var loginWithAddedPassword = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = googleEmail, password = "GooglePassword123!" });
        Assert.Equal(HttpStatusCode.OK, loginWithAddedPassword.StatusCode);
    }

    private sealed record MessageBody(string Message);
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
