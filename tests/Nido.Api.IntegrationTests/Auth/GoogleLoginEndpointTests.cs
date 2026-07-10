using Nido.Application.Auth.Google.Login;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class GoogleLoginEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public GoogleLoginEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GoogleLogin_NewUser_ReturnsOkWithAccessTokenAndIsNewUserTrue()
    {
        var email = $"google-new-{Guid.NewGuid()}@test.com";
        const string googleName = "Ada Lovelace";
        const string googlePicture = "https://lh3.googleusercontent.com/a/ada";
        var client = CreateClientWithFakeValidator(
            new GooglePayload(email, "google-123", googleName, googlePicture));

        var response = await client.PostAsJsonAsync("/api/auth/google-login", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GoogleLoginBody>();
        Assert.NotNull(body);
        Assert.True(body!.IsNewUser);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.True(HasRefreshTokenCookie(response));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var profileResponse = await client.GetFromJsonAsync<ProfileBody>("/api/perfiles");

        Assert.NotNull(profileResponse);
        Assert.Equal(googleName, profileResponse!.Nombre);
        Assert.Equal(googlePicture, profileResponse.FotoUrl);
    }

    [Fact]
    public async Task GoogleLogin_ExistingGoogleUser_ReturnsOkWithIsNewUserFalse()
    {
        var email = $"google-existing-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            await repo.CreateUserWithGoogleAsync(
                new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google User", email, "google", "google-456"),
                CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(
            new GooglePayload(
                email,
                "google-456",
                Picture: "https://lh3.googleusercontent.com/a/existing-user"));
        var response = await client.PostAsJsonAsync("/api/auth/google-login", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GoogleLoginBody>();
        Assert.NotNull(body);
        Assert.False(body!.IsNewUser);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var profileResponse = await client.GetFromJsonAsync<ProfileBody>("/api/perfiles");

        Assert.NotNull(profileResponse);
        Assert.Null(profileResponse!.FotoUrl);
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_ReturnsUnauthorized()
    {
        var client = CreateClientWithFakeValidator(null, shouldThrow: true);
        var response = await client.PostAsJsonAsync("/api/auth/google-login", new { idToken = "invalid-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("INVALID_GOOGLE_TOKEN", problem.Title);
    }

    [Fact]
    public async Task GoogleLogin_PasswordOnlyAccount_ReturnsConflict()
    {
        var email = $"google-conflict-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Password User", email, hasher.Hash(password), "M", null, true, CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-789"));
        var response = await client.PostAsJsonAsync("/api/auth/google-login", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("ACCOUNT_EXISTS_WITH_PASSWORD", problem.Title);
    }

    [Fact]
    public async Task GoogleLogin_EmailMatchesDifferentGoogleId_ReturnsUnauthorized()
    {
        var email = $"google-mismatch-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google User", email, "google", "google-existing"), CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-new"));
        var response = await client.PostAsJsonAsync("/api/auth/google-login", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("GOOGLE_ACCOUNT_MISMATCH", problem!.Title);
    }

    private HttpClient CreateClientWithFakeValidator(GooglePayload? payload, bool shouldThrow = false)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGoogleTokenValidator>();
                services.AddSingleton<IGoogleTokenValidator>(new FakeGoogleValidator(payload, shouldThrow));
            });
        }).CreateClient();
    }

    private static bool HasRefreshTokenCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) &&
        values.Any(v => v.StartsWith("refreshToken="));

    private sealed class FakeGoogleValidator : IGoogleTokenValidator
    {
        private readonly GooglePayload? _payload;
        private readonly bool _shouldThrow;

        public FakeGoogleValidator(GooglePayload? payload, bool shouldThrow)
        {
            _payload = payload;
            _shouldThrow = shouldThrow;
        }

        public Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            if (_shouldThrow || _payload is null)
                throw new Exception("Invalid token");
            return Task.FromResult(_payload);
        }
    }

    private sealed record GoogleLoginBody(
        string AccessToken,
        bool IsNewUser,
        string Plan,
        string SubscriptionStatus,
        DateTime? TrialEndsAt);
    private sealed record ProfileBody(string Nombre, string? FotoUrl);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
