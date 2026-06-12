using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
        var register = await _client.PostAsync("/api/auth/register", registerContent);
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

    [Fact]
    public async Task GetManual_WhenAnonymous_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/productos/manual");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetManual_WhenOtherHouseholdHasStock_OnlyReturnsCurrentHouseholdItems()
    {
        var currentUser = await RegisterAndAuthenticateAsync(_client, "manual-current");
        using var otherClient = _factory.CreateClient();
        var otherUser = await RegisterAndAuthenticateAsync(otherClient, "manual-other");

        var currentProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var currentStockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.AddRange(
                new Producto { Id = currentProductId, Nombre = "Arroz", CodigoBarras = "779-current" },
                new Producto { Id = otherProductId, Nombre = "Cafe", CodigoBarras = "779-other" });
            db.StockHogars.AddRange(
                new StockHogar
                {
                    Id = currentStockId,
                    HogarId = currentUser.HogarId,
                    ProductoId = currentProductId,
                    CargadoPor = currentUser.UsuarioId,
                    UpdatedBy = currentUser.UsuarioId,
                    CantidadActual = 2m,
                    UnidadMedida = "kg",
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                },
                new StockHogar
                {
                    Id = Guid.NewGuid(),
                    HogarId = otherUser.HogarId,
                    ProductoId = otherProductId,
                    CargadoPor = otherUser.UsuarioId,
                    UpdatedBy = otherUser.UsuarioId,
                    CantidadActual = 1m,
                    UnidadMedida = "unidad",
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/productos/manual");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ManualProductBody>>();
        var item = Assert.Single(body!);
        Assert.Equal(currentStockId, item.StockHogarId);
        Assert.Equal("Arroz", item.Nombre);
    }

    [Fact]
    public async Task Create_WhenCatalogProductExists_PersistsStockForCurrentHouseholdAndAppearsInManualList()
    {
        var currentUser = await RegisterAndAuthenticateAsync(_client, "catalog-create");
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.CategoriasProductos.Add(new CategoriasProducto { Id = categoryId, Nombre = "Despensa", TtlDias = 30 });
            db.Productos.Add(new Producto
            {
                Id = productId,
                Nombre = "Tomate Triturado",
                CategoriaId = categoryId,
                ImagenUrl = "products/tomate.webp"
            });
            await db.SaveChangesAsync();
        }

        var createResponse = await _client.PostAsJsonAsync("/api/productos", new
        {
            nombre = "Tomate triturado",
            categoriaId = categoryId,
            ubicacion = "Alacena",
            cantidad = 2m,
            unidadMedida = "unidad",
            fechaVencimiento = "2026-12-31T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var stock = await verifyDb.StockHogars.SingleAsync(x => x.HogarId == currentUser.HogarId && x.ProductoId == productId);
            Assert.Equal(2m, stock.CantidadActual);
            Assert.Equal("Alacena", stock.Ubicacion);
        }

        var manualResponse = await _client.GetAsync("/api/productos/manual");

        Assert.Equal(HttpStatusCode.OK, manualResponse.StatusCode);
        var manualItems = await manualResponse.Content.ReadFromJsonAsync<List<ManualProductBody>>();
        var created = manualItems!.Single(item => item.ProductoId == productId);
        Assert.Equal("Tomate Triturado", created.Nombre);
        Assert.Equal("Alacena", created.Ubicacion);
        Assert.Equal(2m, created.Cantidad);
    }

    [Fact]
    public async Task Create_WhenCatalogProductDoesNotExist_Returns400()
    {
        await RegisterAndAuthenticateAsync(_client, "catalog-missing");

        var response = await _client.PostAsJsonAsync("/api/productos", new
        {
            nombre = "Producto inventado",
            categoriaId = Guid.NewGuid(),
            ubicacion = "Alacena",
            cantidad = 1m,
            unidadMedida = "unidad"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("Validation error", problem.Title);
    }

    private async Task<AuthenticatedUser> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return new AuthenticatedUser(body.UsuarioId, body.HogarId, body.AccessToken);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ProductoBody(Guid Id, string Nombre, string? CodigoBarras, string? Imagen, string? CategoriaNombre, int? TtlDias);
    private sealed record AuthenticatedUser(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ManualProductBody(Guid StockHogarId, Guid ProductoId, string Nombre, Guid? CategoriaId, string? CategoriaNombre, string? CodigoBarras, string? ImagenUrl, string Ubicacion, decimal Cantidad, string? UnidadMedida, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
