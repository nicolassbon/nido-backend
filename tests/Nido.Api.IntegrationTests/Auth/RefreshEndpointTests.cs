using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class RefreshEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public RefreshEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_ValidCookie_ReturnsOkWithNewAccessToken()
    {
        var email = $"refresh-ok-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Test User", email, hasher.Hash(password), "M", null, CancellationToken.None);
        }

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var cookieValue = ExtractRefreshTokenCookie(loginResponse);
        Assert.NotNull(cookieValue);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={cookieValue}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RefreshBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("MISSING_REFRESH_TOKEN", problem.Title);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        var email = $"refresh-expired-{Guid.NewGuid()}@test.com";
        string? rawToken = null;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (userId, _) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Test User", email, "hash", "M", null, CancellationToken.None);

            rawToken = jwt.GenerateRefreshToken();
            var tokenHash = jwt.HashRefreshToken(rawToken);
            await repo.AddRefreshTokenAsync(userId, tokenHash, DateTime.UtcNow.AddDays(-1), CancellationToken.None);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={rawToken}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("INVALID_REFRESH_TOKEN", problem.Title);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=totally-unknown-token-value");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem!.Status);
        Assert.Equal("INVALID_REFRESH_TOKEN", problem.Title);
    }

    private static string? ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var cookie = values.FirstOrDefault(v => v.StartsWith("refreshToken="));
            if (cookie is not null)
            {
                var tokenPart = cookie.Split(';')[0];
                return tokenPart["refreshToken=".Length..];
            }
        }
        return null;
    }

    private sealed record RefreshBody(string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
