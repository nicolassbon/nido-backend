using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Recetas;

public sealed class RecetasEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public RecetasEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = $"receta-{Guid.NewGuid():N}@test.com";
        using var req = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var res  = await _client.PostAsync("/api/auth/register", req);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private async Task<Guid> SeedRecetaAsync(string nombre = "Arroz blanco")
    {
        var id = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        db.Recetas.Add(new Receta
        {
            Id               = id,
            Nombre           = nombre,
            Descripcion      = "Receta de prueba",
            Dificultad       = "Facil",
            Porciones        = 2,
            TiempoCoccionMin = 20,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task GetAll_SinAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/recetas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ConAuth_Returns200YListaConVecesCocinada()
    {
        await AuthenticateAsync();
        await SeedRecetaAsync("Fideos con salsa");

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        Assert.NotNull(lista);
        Assert.NotEmpty(lista!);
        Assert.All(lista!, r => Assert.True(r.VecesCocinada >= 0));
    }

    [Fact]
    public async Task GetById_CuandoExiste_Returns200ConVecesCocinada()
    {
        await AuthenticateAsync();
        var recetaId = await SeedRecetaAsync("Milanesas");

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaBody>();
        Assert.NotNull(body);
        Assert.Equal(recetaId, body!.Id);
        Assert.Equal(0, body.VecesCocinada);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Returns404()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/recetas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cocinar_SinAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync($"/api/recetas/{Guid.NewGuid()}/cocinar", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cocinar_CuandoRecetaNoExiste_Returns404()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync($"/api/recetas/{Guid.NewGuid()}/cocinar", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cocinar_PrimeraVez_Returns200ConVecesCocinada1()
    {
        await AuthenticateAsync();
        var recetaId = await SeedRecetaAsync("Pollo al horno");

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CocinarBody>();
        Assert.NotNull(body);
        Assert.Equal(recetaId, body!.RecetaId);
        Assert.Equal(1, body.VecesCocinada);
    }

    [Fact]
    public async Task Cocinar_SegundaVez_IncrementaVecesCocinada()
    {
        await AuthenticateAsync();
        var recetaId = await SeedRecetaAsync("Revuelto gramajo");

        await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });
        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CocinarBody>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.VecesCocinada);
    }

    [Fact]
    public async Task Cocinar_SeRefleja_EnGetById()
    {
        await AuthenticateAsync();
        var recetaId = await SeedRecetaAsync("Ensalada cesar");

        await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        var getResponse = await _client.GetAsync($"/api/recetas/{recetaId}");
        var body = await getResponse.Content.ReadFromJsonAsync<RecetaBody>();
        Assert.Equal(1, body!.VecesCocinada);
    }

    [Fact]
    public async Task Cocinar_SeRefleja_EnGetAll()
    {
        await AuthenticateAsync();
        var recetaId = await SeedRecetaAsync("Tarta caprese");

        await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        var getResponse = await _client.GetAsync("/api/recetas");
        var lista = await getResponse.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.FirstOrDefault(r => r.Id == recetaId);
        Assert.NotNull(receta);
        Assert.Equal(1, receta!.VecesCocinada);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record RecetaBody(Guid Id, string Nombre, int VecesCocinada);
    private sealed record CocinarBody(Guid RecetaId, int VecesCocinada);
}
