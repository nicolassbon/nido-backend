using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingWellnessQueryTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public OnboardingWellnessQueryTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWellness_WhenRestrictionsAndMetasSaved_ReturnsCorrectData()
    {
        using var registerContent = RegisterMultipartRequest.Create("Wellness User", "wellness@test.com", "Password123!", "F");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(registerResult);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        
        var restriccionId = await db.RestriccionesCatalogo.Select(x => x.Id).FirstOrDefaultAsync();
        var metaId = await db.MetasCatalogo.Select(x => x.Id).FirstOrDefaultAsync();

        Assert.NotEqual(Guid.Empty, restriccionId);
        Assert.NotEqual(Guid.Empty, metaId);

        var step4Response = await _client.PatchAsJsonAsync("/api/onboarding/step-4", new
        {
            skip = false,
            restriccionIds = new[] { restriccionId },
            metaIds = new[] { metaId },
            usuarioId = registerResult.UsuarioId,
            hogarId = registerResult.HogarId
        });
        Assert.Equal(HttpStatusCode.NoContent, step4Response.StatusCode);

        var getWellnessResponse = await _client.GetAsync("/api/onboarding/wellness");
        Assert.Equal(HttpStatusCode.OK, getWellnessResponse.StatusCode);

        var wellnessData = await getWellnessResponse.Content.ReadFromJsonAsync<WellnessResponse>();
        Assert.NotNull(wellnessData);
        Assert.Contains(restriccionId, wellnessData.RestriccionIds);
        Assert.Contains(metaId, wellnessData.MetaIds);
    }

    [Fact]
    public async Task GetWellness_WhenNoneSaved_ReturnsEmptyLists()
    {
        using var registerContent = RegisterMultipartRequest.Create("No Wellness User", "nowellness@test.com", "Password123!", "M");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(registerResult);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        var getWellnessResponse = await _client.GetAsync("/api/onboarding/wellness");
        Assert.Equal(HttpStatusCode.OK, getWellnessResponse.StatusCode);

        var wellnessData = await getWellnessResponse.Content.ReadFromJsonAsync<WellnessResponse>();
        Assert.NotNull(wellnessData);
        Assert.Empty(wellnessData.RestriccionIds);
        Assert.Empty(wellnessData.MetaIds);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record WellnessResponse(List<Guid> RestriccionIds, List<Guid> MetaIds);
}
