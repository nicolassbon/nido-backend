using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class LoginEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public LoginEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithAccessTokenAndRefreshCookie()
    {
        var email = $"login-ok-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);

        }

        var response = await _client.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));

        Assert.True(HasRefreshTokenCookie(response));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"login-wrong-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
        }

        var response = await _client.PostAsJsonAsync("/auth/login", new { email, password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email = "missing@test.com", password = "Password123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_GoogleOnlyAccount_ReturnsConflictWithGoogleContract()
    {
        var email = $"login-google-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var (userId, _) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google User", email, "google", "google-123"), CancellationToken.None);
        }

        var response = await _client.PostAsJsonAsync("/auth/login", new { email, password = "AnyPassword1!" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("ACCOUNT_EXISTS_WITH_GOOGLE", problem.Title);
        Assert.Equal("This account was created with Google. Use Google login or set a password from account linking.", problem.Detail);
    }

    private static bool HasRefreshTokenCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) &&
        values.Any(v => v.StartsWith("refreshToken="));

    private sealed record LoginBody(string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
