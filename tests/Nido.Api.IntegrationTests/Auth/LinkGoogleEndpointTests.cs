using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class LinkGoogleEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public LinkGoogleEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LinkGoogle_AuthenticatedUser_LinksGoogleAndReturnsTokens()
    {
        var email = $"link-ok-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);

        var accessToken = await LoginAsync(email, password);
        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-1"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LinkGoogleBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.True(HasRefreshTokenCookie(response));
    }

    [Fact]
    public async Task LinkGoogle_NoJwt_ReturnsUnauthorized()
    {
        var client = CreateClientWithFakeValidator(new GooglePayload("any@test.com", "google-no-jwt"));

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_InvalidGoogleToken_ReturnsUnauthorized()
    {
        var email = $"link-invalid-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);
        var accessToken = await LoginAsync(email, password);

        var client = CreateClientWithFakeValidator(null, shouldThrow: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "invalid-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("INVALID_GOOGLE_TOKEN", problem.Title);
    }

    [Fact]
    public async Task LinkGoogle_AlreadyLinked_ReturnsConflict()
    {
        var email = $"link-conflict-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            (userId, _) = await repo.CreateUserWithPasswordAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);

            var db = scope.ServiceProvider.GetRequiredService<Nido.Infrastructure.Persistence.NidoDbContext>();
            var user = await db.Usuarios.FindAsync(userId);
            user!.OauthProvider = "google";
            user.OauthId = "google-link-4";
            await db.SaveChangesAsync();
        }

        var accessToken = await LoginAsync(email, password);
        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-4"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("ACCOUNT_ALREADY_LINKED", problem.Title);
    }

    [Fact]
    public async Task LinkGoogle_GoogleEmailDoesNotMatchAuthenticatedUser_ReturnsUnauthorized()
    {
        var email = $"link-email-mismatch-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);
        var accessToken = await LoginAsync(email, password);

        var client = CreateClientWithFakeValidator(new GooglePayload("other@test.com", "google-email-mismatch"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("GOOGLE_EMAIL_MISMATCH", problem!.Title);
    }

    [Fact]
    public async Task LinkGoogle_GoogleIdLinkedToDifferentUser_ReturnsConflict()
    {
        var email = $"link-owner-{Guid.NewGuid()}@test.com";
        var otherEmail = $"link-other-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";
        const string googleId = "google-owned-by-other";

        await RegisterUserAsync(email, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Other User", otherEmail, "google", googleId), CancellationToken.None);
        }

        var accessToken = await LoginAsync(email, password);
        var client = CreateClientWithFakeValidator(new GooglePayload(email, googleId));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("GOOGLE_ACCOUNT_ALREADY_LINKED", problem!.Title);
    }


    private async Task RegisterUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        return body!.AccessToken;
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

    private sealed record LinkGoogleBody(string AccessToken);
    private sealed record LoginBody(string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
