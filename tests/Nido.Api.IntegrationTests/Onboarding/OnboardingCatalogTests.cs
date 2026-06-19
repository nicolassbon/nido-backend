using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests.Auth;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingCatalogTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public OnboardingCatalogTests(NidoTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CatalogEndpoints_WhenTokenIsMissing_ReturnUnauthorized()
    {
        var preferences = await _client.GetAsync("/api/onboarding/preferencias-alimentarias");
        var allergies = await _client.GetAsync("/api/onboarding/alergias");
        var goals = await _client.GetAsync("/api/onboarding/metas");
        var equipmentCatalog = await _client.GetAsync("/api/electrodomesticos/catalogo");

        Assert.Equal(HttpStatusCode.Unauthorized, preferences.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, allergies.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, goals.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, equipmentCatalog.StatusCode);
    }

    [Fact]
    public async Task GetAlergias_WhenQueryProvided_ReturnsFilteredAllergyCatalog()
    {
        using var registerContent = RegisterMultipartRequest.Create("Catalog User", "catalog@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.GetAsync("/api/onboarding/alergias?q=man");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<RestriccionCatalogoBody>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items!);
        Assert.All(items!, item => Assert.Equal("alergia", item.Tipo));
        Assert.Contains(items!, item => item.Nombre.Contains("Man", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record RestriccionCatalogoBody(Guid Id, string Nombre, string Tipo);
}
