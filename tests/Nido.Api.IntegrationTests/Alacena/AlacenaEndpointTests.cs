using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Alacena;

public sealed class AlacenaEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public AlacenaEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductos_WhenAnonymous_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/alacena/productos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task CreateProducto_WhenBarcodeMatchesExistingProduct_ReusesExistingProductAndPersistsHouseholdScopedStock()
    {
        var user = await RegisterAndAuthenticateAsync(_client, "alacena-manual-create");
        var existingProductId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = existingProductId,
                Nombre = "Yerba catalogada",
                CodigoBarras = "779999000111",
                ImagenUrl = null
            });
            await db.SaveChangesAsync();
        }

        var createResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Yerba manual",
            codigoBarras = "779999000111",
            imagen = "https://img.test/yerba.png",
            ubicacion = "Alacena",
            cantidad = 1.5m,
            unidadMedida = "kg",
            fechaVencimiento = "2026-12-01",
            estaAbierto = false,
            porcentajeConsumido = 0m
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(x => x.HogarId == user.HogarId);
        Assert.Equal(existingProductId, stock.ProductoId);
        Assert.Equal(1.5m, stock.CantidadActual);

        var product = await verifyDb.Productos.SingleAsync(x => x.Id == existingProductId);
        Assert.Equal("https://img.test/yerba.png", product.ImagenUrl);
    }

    [Fact]
    public async Task CreateProducto_WhenDateIsInvalid_ReturnsBadRequest()
    {
        await RegisterAndAuthenticateAsync(_client, "alacena-invalid-date");

        var response = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Yerba",
            codigoBarras = "779999",
            ubicacion = "Alacena",
            cantidad = 1,
            fechaVencimiento = "31/12/2026",
            estaAbierto = false,
            porcentajeConsumido = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("INVALID_STOCK_ITEM_DATE", problem.Title);
    }

    [Fact]
    public async Task GetProducto_WhenStockBelongsToAnotherHousehold_ReturnsNotFound()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "alacena-get-owner");
        using var outsiderClient = _factory.CreateClient();
        _ = await RegisterAndAuthenticateAsync(outsiderClient, "alacena-get-outsider");
        var stockId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto { Id = productId, Nombre = "Queso" });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = owner.HogarId,
                ProductoId = productId,
                CargadoPor = owner.UsuarioId,
                UpdatedBy = owner.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "kg",
                Ubicacion = "Heladera",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await outsiderClient.GetAsync($"/api/alacena/productos/{stockId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProducto_WhenDateIsInvalid_ReturnsBadRequest()
    {
        var user = await RegisterAndAuthenticateAsync(_client, "alacena-update-invalid-date");
        var stockId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto { Id = productId, Nombre = "Manteca" });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = user.HogarId,
                ProductoId = productId,
                CargadoPor = user.UsuarioId,
                UpdatedBy = user.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "kg",
                Ubicacion = "Heladera",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PatchAsJsonAsync($"/api/alacena/productos/{stockId}", new
        {
            nombre = "Manteca",
            cantidad = 1m,
            ubicacion = "Heladera",
            unidadMedida = "kg",
            fechaVencimiento = "31/12/2026",
            estaAbierto = false,
            porcentajeConsumido = 0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("INVALID_STOCK_ITEM_DATE", problem.Title);
    }

    [Fact]
    public async Task UpdateProducto_WhenStockBelongsToAnotherHousehold_ReturnsNotFoundAndDoesNotModifyTarget()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "alacena-update-owner");
        using var outsiderClient = _factory.CreateClient();
        _ = await RegisterAndAuthenticateAsync(outsiderClient, "alacena-update-outsider");
        var stockId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto { Id = productId, Nombre = "Leche" });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = owner.HogarId,
                ProductoId = productId,
                CargadoPor = owner.UsuarioId,
                UpdatedBy = owner.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "lt",
                Ubicacion = "Heladera",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await outsiderClient.PatchAsJsonAsync($"/api/alacena/productos/{stockId}", new
        {
            nombre = "Leche editada",
            cantidad = 5m,
            ubicacion = "Freezer",
            unidadMedida = "lt",
            estaAbierto = true,
            porcentajeConsumido = 20m
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(x => x.Id == stockId);
        Assert.Equal(owner.HogarId, stock.HogarId);
        Assert.Equal(1m, stock.CantidadActual);
        Assert.Equal("Heladera", stock.Ubicacion);
        Assert.False(stock.EstaAbierto);
    }

    [Fact]
    public async Task DeleteProducto_WhenStockBelongsToAnotherHousehold_ReturnsNotFoundAndDoesNotRemoveTarget()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "alacena-delete-owner");
        using var outsiderClient = _factory.CreateClient();
        _ = await RegisterAndAuthenticateAsync(outsiderClient, "alacena-delete-outsider");
        var stockId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto { Id = productId, Nombre = "Pan" });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = owner.HogarId,
                ProductoId = productId,
                CargadoPor = owner.UsuarioId,
                UpdatedBy = owner.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "unidad",
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await outsiderClient.DeleteAsync($"/api/alacena/productos/{stockId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.True(await verifyDb.StockHogars.AnyAsync(x => x.Id == stockId && x.HogarId == owner.HogarId));
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
    private sealed record AuthenticatedUser(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
    private sealed record StockItemBody(Guid Id, Guid ProductoId, string Nombre, string? Imagen, string? CodigoBarras, string Ubicacion, decimal Cantidad, string? UnidadMedida, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
}
