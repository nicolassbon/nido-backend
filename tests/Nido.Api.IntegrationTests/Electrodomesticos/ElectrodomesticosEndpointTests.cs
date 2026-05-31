using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests.Auth;

namespace Nido.Api.IntegrationTests.Electrodomesticos;

public sealed class ElectrodomesticosEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public ElectrodomesticosEndpointTests(NidoTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsOnlyCurrentHouseholdEquipment()
    {
        var userA = await RegisterAsync("equip-list-a");
        var userB = await RegisterAsync("equip-list-b");

        Authenticate(userA);
        var createA = await _client.PostAsJsonAsync("/electrodomesticos", new
        {
            nombre = "Heladera",
            tipo = "Cocina",
            estado = "Activo"
        });

        Authenticate(userB);
        var createB = await _client.PostAsJsonAsync("/electrodomesticos", new
        {
            nombre = "Lavarropas",
            tipo = "Lavadero",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, createA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createB.StatusCode);

        Authenticate(userA);
        var response = await _client.GetAsync("/electrodomesticos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<ElectrodomesticoBody>>();
        Assert.NotNull(body);
        var item = Assert.Single(body!);
        Assert.Equal(userA.HogarId, item.HogarId);
        Assert.Equal("Heladera", item.Nombre);
    }

    [Fact]
    public async Task Post_WhenBodyContainsAnotherHousehold_ReturnsForbidden()
    {
        var userA = await RegisterAsync("equip-forbid-a");
        var userB = await RegisterAsync("equip-forbid-b");

        Authenticate(userA);
        var response = await _client.PostAsJsonAsync("/electrodomesticos", new
        {
            hogarId = userB.HogarId,
            nombre = "Horno",
            tipo = "Cocina",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<RegisterBody> RegisterAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var response = await _client.PostAsync("/auth/register", registerContent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        return body!;
    }

    private void Authenticate(RegisterBody user)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user.AccessToken);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);

    private sealed record ElectrodomesticoBody(
        Guid Id,
        Guid HogarId,
        string Nombre,
        string? Tipo,
        string? Estado,
        string? Marca,
        string? ImagenUrl);
}
