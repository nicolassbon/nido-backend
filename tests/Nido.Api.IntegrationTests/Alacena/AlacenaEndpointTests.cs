using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Alacena;

public sealed class AlacenaEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AlacenaEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
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
            nombre = "Yerba editada",
            cantidad = 2,
            ubicacion = "Heladera",
            unidadMedida = "kg",
            fechaVencimiento = "2026-12-02",
            estaAbierto = true,
            porcentajeConsumido = 25
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patched = await patchResponse.Content.ReadFromJsonAsync<StockItemBody>();
        Assert.NotNull(patched);
        Assert.Equal("Yerba editada", patched!.Nombre);
        Assert.Equal("kg", patched.UnidadMedida);

        var deleteResponse = await _client.DeleteAsync($"/api/alacena/productos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetProductos_WhenProductStoresSpacesKey_ReturnsResolvedPublicImageUrl()
    {
        var email = $"alacena-image-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Image User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var regBody = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regBody!.AccessToken);

        var productId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        const string imageKey = "products/test-image.webp";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productId,
                Nombre = "Yerba con foto",
                ImagenUrl = imageKey
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = regBody.HogarId,
                ProductoId = productId,
                CargadoPor = regBody.UsuarioId,
                UpdatedBy = regBody.UsuarioId,
                CreatedAt = DateTime.UtcNow,
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0
            });
            await db.SaveChangesAsync();
        }

        var listResponse = await _client.GetAsync("/api/alacena/productos");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<List<StockItemBody>>();
        var item = Assert.Single(list!, x => x.Id == stockId);
        Assert.NotNull(item.Imagen);
        Assert.StartsWith("https://", item.Imagen, StringComparison.Ordinal);
        Assert.EndsWith(imageKey, item.Imagen, StringComparison.Ordinal);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record StockItemBody(Guid Id, Guid ProductoId, string Nombre, string? Imagen, string? CodigoBarras, string Ubicacion, decimal Cantidad, string? UnidadMedida, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
}
