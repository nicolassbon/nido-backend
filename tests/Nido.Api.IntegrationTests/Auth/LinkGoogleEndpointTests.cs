using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;

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
    public async Task LinkGoogle_ValidTokenAndPassword_ReturnsOkWithAccessTokenAndCookie()
    {
        var email = $"link-ok-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithDefaultHouseholdAsync("Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-1"));
        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token", password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LinkGoogleBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.True(HasRefreshTokenCookie(response));
    }

    [Fact]
    public async Task LinkGoogle_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"link-wrong-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithDefaultHouseholdAsync("Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-2"));
        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token", password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_NonExistentUser_ReturnsNotFound()
    {
        const string email = "missing-link@test.com";
        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-3"));
        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token", password = "Password123!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_InvalidToken_ReturnsUnauthorized()
    {
        var email = $"link-invalid-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithDefaultHouseholdAsync("Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
        }

        var client = CreateClientWithFakeValidator(null, shouldThrow: true);
        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "invalid-token", password });

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

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var (userId, _) = await repo.CreateUserWithDefaultHouseholdAsync("Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);

            var user = await db.Usuarios.FindAsync(userId);
            user!.OauthProvider = "google";
            user.OauthId = "google-link-4";
            await db.SaveChangesAsync();
        }

        var client = CreateClientWithFakeValidator(new GooglePayload(email, "google-link-4"));
        var response = await client.PostAsJsonAsync("/auth/link-google", new { idToken = "valid-token", password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal("ACCOUNT_ALREADY_LINKED", problem.Detail);
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
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
