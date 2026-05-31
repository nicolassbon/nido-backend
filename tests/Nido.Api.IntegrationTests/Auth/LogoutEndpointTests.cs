using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class LogoutEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public LogoutEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_WithValidCookie_ReturnsNoContentAndDeletesCookie()
    {
        var email = $"logout-ok-{Guid.NewGuid()}@test.com";
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("Cookie", $"refreshToken={cookieValue}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("refreshToken="))
            : null;
        Assert.NotNull(setCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            Assert.Equal(0, await db.RefreshTokens.CountAsync());
        }
    }

    [Fact]
    public async Task Logout_WithoutCookie_ReturnsNoContent()
    {
        var response = await _client.PostAsync("/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
}
