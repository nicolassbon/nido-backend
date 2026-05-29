using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth;
using Nido.Application.Common.Security;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class RegisterEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public RegisterEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsCreatedAndJwtClaims()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Nico", email = "nico@test.com", password = "Password123!", sexo = "M" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == "usuarioId" && c.Value == body.UsuarioId.ToString());
        Assert.Contains(token.Claims, c => c.Type == "hogarId" && c.Value == body.HogarId.ToString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var payload = new { nombre = "Nico", email = "nico-dup@test.com", password = "Password123!", sexo = "M" };

        var first = await _client.PostAsJsonAsync("/auth/register", payload);
        var second = await _client.PostAsJsonAsync("/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task Register_MissingRequiredField_ReturnsBadRequest()
    {
        const string missingNombreJson = """
            {
              "email": "missing-field@test.com",
              "password": "Password123!",
              "sexo": "F"
            }
            """;

        using var content = new StringContent(missingNombreJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task Register_GoogleOnlyAccount_AddsPasswordAndReturnsCreatedWithTokens()
    {
        var email = $"google-add-pw-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var (userId, hogarId) = await repo.CreateUserWithDefaultHouseholdAsync("Google User", email, "placeholder", "U", null, CancellationToken.None);

            var user = await db.Usuarios.FindAsync(userId);
            user!.PasswordHash = null;
            user.OauthProvider = "google";
            user.OauthId = "google-123";
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            nombre = "Google User",
            email,
            password = "Password123!",
            sexo = "U"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.NotEqual(Guid.Empty, body.UsuarioId);
        Assert.NotEqual(Guid.Empty, body.HogarId);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values) && values.Any(v => v.StartsWith("refreshToken=")));
    }

    [Fact]
    public async Task Onboarding_WhenUnexpectedExceptionIsThrown_ReturnsInternalServerErrorProblemDetails()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserContext>();
                services.AddScoped<ICurrentUserContext, ThrowingCurrentUserContext>();
            });
        }).CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            nombre = "Err User",
            email = "error-user@test.com",
            password = "Password123!",
            sexo = "M"
        });
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerBody!.AccessToken);

        var response = await client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem!.Status);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);

    private sealed class ThrowingCurrentUserContext : ICurrentUserContext
    {
        public Guid UsuarioId => throw new Exception("boom");
        public Guid HogarId => throw new Exception("boom");
    }
}
