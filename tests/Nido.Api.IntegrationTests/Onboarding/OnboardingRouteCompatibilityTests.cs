using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingRouteCompatibilityTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public OnboardingRouteCompatibilityTests(NidoTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PublicStepRoutes_WhenAuthenticated_AcceptRequestsOnExistingContracts()
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Compat", email = "compat@test.com", password = "Password123!", sexo = "M" });
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var step2 = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });
        var step3 = await _client.PatchAsJsonAsync("/onboarding/step-3", new { skip = true });
        var step4 = await _client.PatchAsJsonAsync("/onboarding/step-4", new { skip = true });

        Assert.Equal(HttpStatusCode.NoContent, step2.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, step3.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, step4.StatusCode);
    }

    [Fact]
    public async Task StepRoutes_WhenForbiddenByForgedIdentity_ReturnProblemDetailsShape()
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Boundary", email = "boundary@test.com", password = "Password123!", sexo = "F" });
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new
        {
            skip = false,
            usuarioId = Guid.NewGuid(),
            hogarId = body.HogarId,
            members = new[] { new { nombre = "X", rol = "child" } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem!.Status);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
