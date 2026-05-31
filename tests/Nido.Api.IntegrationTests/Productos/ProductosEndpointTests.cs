using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Productos;

public sealed class ProductosEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public ProductosEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = $"prod-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    [Fact]
    public async Task GetByBarcode_WhenExists_Returns200AndExpectedContract()
    {
        await AuthenticateAsync();

        var codigo = $"779-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var categoria = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = "Despensa", TtlDias = 10 };
            var producto = new Producto { Id = Guid.NewGuid(), Nombre = "Fideos", CodigoBarras = codigo, Categoria = categoria };
            db.CategoriasProductos.Add(categoria);
            db.Productos.Add(producto);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/productos/barcode/{codigo}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductoBody>();
        Assert.NotNull(body);
        Assert.Equal(codigo, body!.CodigoBarras);
        Assert.Equal("Despensa", body.CategoriaNombre);
    }

    [Fact]
    public async Task GetByBarcode_WhenNotExists_Returns404()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/productos/barcode/missing-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ProductoBody(Guid Id, string Nombre, string? CodigoBarras, string? Imagen, string? CategoriaNombre, int? TtlDias);
}
