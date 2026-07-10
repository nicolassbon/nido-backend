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

        var categoriaNombre = $"Despensa-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var categoria = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = categoriaNombre, TtlDias = 10 };
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
        Assert.Equal(categoriaNombre, body.CategoriaNombre);
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
        var body = await response.Content.ReadFromJsonAsync<List<GetProductManualBody>>();
        var item = Assert.Single(body!);
        Assert.Equal(currentStockId, item.StockHogarId);
        Assert.Equal("Arroz", item.Nombre);
    }

    [Fact]
    public async Task Create_WhenCatalogNameMatchesExistingProduct_CreatesDedicatedStockForCurrentHouseholdAndAppearsInManualList()
    {
        var currentUser = await RegisterAndAuthenticateAsync(_client, "catalog-create");
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.CategoriasProductos.Add(new CategoriasProducto { Id = categoryId, Nombre = $"Despensa-{Guid.NewGuid():N}", TtlDias = 30 });
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
            fechaVencimiento = "2026-12-31"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdBody = await createResponse.Content.ReadFromJsonAsync<CreateProductoBody>();
        Assert.NotNull(createdBody);
        Assert.NotEqual(productId, createdBody!.ProductoId);

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var stock = await verifyDb.StockHogars.SingleAsync(x => x.HogarId == currentUser.HogarId && x.ProductoId == createdBody.ProductoId);
            Assert.Equal(2m, stock.CantidadActual);
            Assert.Equal("Alacena", stock.Ubicacion);
        }

        var manualResponse = await _client.GetAsync("/api/productos/manual");

        Assert.Equal(HttpStatusCode.OK, manualResponse.StatusCode);
        var manualItems = await manualResponse.Content.ReadFromJsonAsync<List<GetProductManualBody>>();
        var created = manualItems!.Single(item => item.ProductoId == createdBody.ProductoId);
        Assert.Equal("Tomate triturado", created.Nombre);
        Assert.Equal("Alacena", created.Ubicacion);
        Assert.Equal(2m, created.Cantidad);
    }

    [Fact]
    public async Task Create_WhenCategoryIsOmitted_CreatesManualProductWithoutCategory()
    {
        await RegisterAndAuthenticateAsync(_client, "catalog-missing");

        var response = await _client.PostAsJsonAsync("/api/productos", new
        {
            nombre = $"Producto sin categoria {Guid.NewGuid():N}",
            ubicacion = "Alacena",
            cantidad = 1m,
            unidadMedida = "unidad"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateProductoBody>();
        Assert.NotNull(body);
        Assert.Null(body!.CategoriaId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var producto = await db.Productos.SingleAsync(x => x.Id == body.ProductoId);
        Assert.Null(producto.CategoriaId);
    }

    [Fact]
    public async Task Create_WhenProductDoesNotExist_CreatesProductoPreservesCategoryAndDateOnly()
    {
        await AuthenticateAsync();

        var categoriaId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.CategoriasProductos.Add(new CategoriasProducto
            {
                Id = categoriaId,
                Nombre = $"Despensa-{Guid.NewGuid():N}",
                TtlDias = 30
            });
            await db.SaveChangesAsync();
        }

        var nombre = $"Producto manual {Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/productos", new
        {
            nombre,
            categoriaId,
            ubicacion = "Alacena",
            cantidad = 2,
            unidadMedida = "unidad",
            fechaVencimiento = "2026-12-01",
            cantidadEnvases = 3
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateProductoBody>();
        Assert.NotNull(body);
        Assert.Equal("2026-12-01", body!.FechaVencimiento);
        Assert.Equal(categoriaId, body.CategoriaId);
        Assert.Equal(3, body.CantidadEnvases);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var producto = await db.Productos.SingleAsync(x => x.Id == body.ProductoId);
            var stock = await db.StockHogars.SingleAsync(x => x.Id == body.StockHogarId);

            Assert.Equal(nombre, producto.Nombre);
            Assert.Null(producto.ImagenUrl);
            Assert.Equal(categoriaId, producto.CategoriaId);
            Assert.Equal(new DateOnly(2026, 12, 1), stock.FechaVencimiento);
            Assert.Equal(3, stock.CantidadEnvases);
        }

        var manualResponse = await _client.GetAsync("/api/productos/manual");

        Assert.Equal(HttpStatusCode.OK, manualResponse.StatusCode);

        var manualBody = await manualResponse.Content.ReadFromJsonAsync<List<GetProductManualBody>>();

        Assert.NotNull(manualBody);

        var createdManualProduct = Assert.Single(manualBody!, x => x.StockHogarId == body.StockHogarId);
        Assert.Equal(3, createdManualProduct.CantidadEnvases);
    }

    [Fact]
    public async Task Create_WithNonExactDateFormat_Returns400()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/productos", new
        {
            nombre = $"Producto manual {Guid.NewGuid():N}",
            categoriaId = Guid.NewGuid(),
            ubicacion = "Alacena",
            cantidad = 2,
            unidadMedida = "unidad",
            fechaVencimiento = "2026-12-1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithSameManualNameAcrossHouseholds_CreatesDedicatedProducts()
    {
        var firstClient = _factory.CreateClient();
        var secondClient = _factory.CreateClient();

        var firstUser = await RegisterAndAuthenticateAsync(firstClient, "manual-product-first");
        var secondUser = await RegisterAndAuthenticateAsync(secondClient, "manual-product-second");
        var nombre = $"Producto manual compartido {Guid.NewGuid():N}";

        var firstResponse = await firstClient.PostAsJsonAsync("/api/productos", new
        {
            nombre,
            categoriaId = (Guid?)null,
            ubicacion = "Alacena",
            cantidad = 1,
            unidadMedida = "unidad"
        });

        var secondResponse = await secondClient.PostAsJsonAsync("/api/productos", new
        {
            nombre,
            categoriaId = (Guid?)null,
            ubicacion = "Alacena",
            cantidad = 1,
            unidadMedida = "unidad"
        });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstBody = await firstResponse.Content.ReadFromJsonAsync<CreateProductoBody>();
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<CreateProductoBody>();

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.NotEqual(firstBody!.ProductoId, secondBody!.ProductoId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var createdProducts = await db.StockHogars
            .Where(x => x.HogarId == firstUser.HogarId || x.HogarId == secondUser.HogarId)
            .Select(x => new { x.HogarId, x.ProductoId })
            .ToListAsync();

        Assert.Contains(createdProducts, x => x.HogarId == firstUser.HogarId && x.ProductoId == firstBody.ProductoId);
        Assert.Contains(createdProducts, x => x.HogarId == secondUser.HogarId && x.ProductoId == secondBody.ProductoId);
    }

    private static async Task<RegisterBody> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return body;
    }

    private sealed record RegisterBody(
        Guid UsuarioId,
        Guid HogarId,
        string AccessToken,
        string Plan,
        string SubscriptionStatus,
        DateTime? TrialEndsAt);
    private sealed record ProductoBody(Guid Id, string Nombre, string? CodigoBarras, string? Imagen, string? CategoriaNombre, int? TtlDias);
    private sealed record CreateProductoBody(Guid StockHogarId, Guid ProductoId, decimal CantidadActual, string UnidadMedida, string? FechaVencimiento, Guid UsuarioIngresoId, string Ubicacion, bool EstaAbierto, decimal PorcentajeConsumido, Guid? CategoriaId, int CantidadEnvases);
    private sealed record GetProductManualBody(Guid StockHogarId, Guid ProductoId, string Nombre, Guid? CategoriaId, string? CategoriaNombre, string? CodigoBarras, string? ImagenUrl, string Ubicacion, decimal Cantidad, string? UnidadMedida, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido, int CantidadEnvases);
}
