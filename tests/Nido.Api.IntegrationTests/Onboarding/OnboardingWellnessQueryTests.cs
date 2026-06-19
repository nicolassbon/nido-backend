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

    [Fact]
    public async Task SaveWellness_WhenSubmittedAgain_ReplacesPreviousSelectionsAndUpdatesQuery()
    {
        using var registerContent = RegisterMultipartRequest.Create("Retry Wellness", "retry-wellness@test.com", "Password123!", "F");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(registerResult);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.AccessToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var restriccionIds = await db.RestriccionesCatalogo.OrderBy(x => x.Nombre).Select(x => x.Id).Take(2).ToListAsync();
        var metaIds = await db.MetasCatalogo.OrderBy(x => x.Nombre).Select(x => x.Id).Take(2).ToListAsync();

        Assert.Equal(2, restriccionIds.Count);
        Assert.Equal(2, metaIds.Count);

        var firstResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-4", new
        {
            skip = false,
            restriccionIds = new[] { restriccionIds[0] },
            metaIds = new[] { metaIds[0] }
        });

        var secondResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-4", new
        {
            skip = false,
            restriccionIds = new[] { restriccionIds[1] },
            metaIds = new[] { metaIds[1] }
        });

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        var userRestricciones = await db.RestriccionesUsuarios
            .Where(x => x.UsuarioId == registerResult.UsuarioId)
            .Select(x => x.RestriccionId)
            .ToListAsync();

        var hogarMetas = await db.HogarMetas
            .Where(x => x.HogarId == registerResult.HogarId)
            .Select(x => x.MetaId)
            .ToListAsync();

        Assert.Equal([restriccionIds[1]], userRestricciones);
        Assert.Equal([metaIds[1]], hogarMetas);

        var queryResponse = await _client.GetAsync("/api/onboarding/wellness");
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var wellnessData = await queryResponse.Content.ReadFromJsonAsync<WellnessResponse>();
        Assert.NotNull(wellnessData);
        Assert.Equal([restriccionIds[1]], wellnessData!.RestriccionIds);
        Assert.Equal([metaIds[1]], wellnessData.MetaIds);

        var state = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == registerResult.UsuarioId && x.HogarId == registerResult.HogarId);
        Assert.False(state.Step4Skipped);
        Assert.NotNull(state.Step4CompletedAt);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record WellnessResponse(List<Guid> RestriccionIds, List<Guid> MetaIds);
}
