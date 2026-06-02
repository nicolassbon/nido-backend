using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests.Auth;

namespace Nido.Api.IntegrationTests.Alacena;

public sealed class AlacenaEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public AlacenaEndpointTests(NidoTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrudProductoStock_PreservesHttpContract()
    {
        var email = $"alacena-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var regBody = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regBody!.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Yerba",
            codigoBarras = "779999",
            imagen = "https://img.test/yerba.png",
            ubicacion = "Alacena",
            cantidad = 1,
            fechaVencimiento = "2026-12-01",
            estaAbierto = false,
            porcentajeConsumido = 0
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<StockItemBody>();
        Assert.NotNull(created);

        var listResponse = await _client.GetAsync("/api/alacena/productos");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<StockItemBody>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list!);

        var patchResponse = await _client.PatchAsJsonAsync($"/api/alacena/productos/{created!.Id}", new
        {
            cantidad = 2,
            ubicacion = "Heladera",
            fechaVencimiento = "2026-12-02",
            estaAbierto = true,
            porcentajeConsumido = 25
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/alacena/productos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record StockItemBody(Guid Id, Guid ProductoId, string Nombre, string? Imagen, string? CodigoBarras, string Ubicacion, decimal Cantidad, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
}
