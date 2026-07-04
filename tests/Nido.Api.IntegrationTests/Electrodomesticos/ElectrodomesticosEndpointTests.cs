using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Electrodomesticos;

public sealed class ElectrodomesticosEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public ElectrodomesticosEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsOnlyCurrentHouseholdEquipment()
    {
        var userA = await RegisterAsync("equip-list-a");
        var userB = await RegisterAsync("equip-list-b");

        Authenticate(userA);
        var createA = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            nombre = "Heladera",
            tipo = "Cocina",
            estado = "Activo"
        });

        Authenticate(userB);
        var createB = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            nombre = "Lavarropas",
            tipo = "Lavadero",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Created, createA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createB.StatusCode);

        Authenticate(userA);
        var response = await _client.GetAsync("/api/electrodomesticos");

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
        var response = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            hogarId = userB.HogarId,
            nombre = "Horno",
            tipo = "Cocina",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByHogar_WhenRequestingAnotherHousehold_ReturnsForbidden()
    {
        var userA = await RegisterAsync("equip-get-hogar-a");
        var userB = await RegisterAsync("equip-get-hogar-b");

        Authenticate(userA);
        var response = await _client.GetAsync($"/api/electrodomesticos/hogar/{userB.HogarId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCatalogo_ReturnsOnlyActiveItemsOrderedByOrden()
    {
        var user = await RegisterAsync("equip-catalog");
        var visibleSecondId = Guid.NewGuid();
        var visibleFirstId = Guid.NewGuid();
        var hiddenId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.ElectrodomesticosCatalogo.AddRange(
                new ElectrodomesticoCatalogo
                {
                    Id = visibleSecondId,
                    Nombre = "Catalog Visible Second",
                    Tipo = "Kitchen",
                    Icono = "second",
                    ImagenUrl = "catalog/second.webp",
                    Orden = 20,
                    Activo = true
                },
                new ElectrodomesticoCatalogo
                {
                    Id = visibleFirstId,
                    Nombre = "Catalog Visible First",
                    Tipo = "Kitchen",
                    Icono = "first",
                    ImagenUrl = "catalog/first.webp",
                    Orden = 10,
                    Activo = true
                },
                new ElectrodomesticoCatalogo
                {
                    Id = hiddenId,
                    Nombre = "Catalog Hidden",
                    Tipo = "Kitchen",
                    Icono = "hidden",
                    ImagenUrl = "catalog/hidden.webp",
                    Orden = 5,
                    Activo = false
                });
            await db.SaveChangesAsync();
        }

        Authenticate(user);
        var response = await _client.GetAsync("/api/electrodomesticos/catalogo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<ElectrodomesticoCatalogBody>>();
        Assert.NotNull(body);

        var visibleItems = body!
            .Where(item => item.Id == visibleFirstId || item.Id == visibleSecondId)
            .ToList();

        Assert.Equal([visibleFirstId, visibleSecondId], visibleItems.Select(item => item.Id).ToArray());
        Assert.DoesNotContain(body!, item => item.Id == hiddenId);
        Assert.All(visibleItems, item =>
        {
            Assert.NotNull(item.ImagenUrl);
            Assert.StartsWith("https://", item.ImagenUrl!, StringComparison.Ordinal);
            Assert.EndsWith($"catalog/{item.Nombre.Split(' ').Last().ToLowerInvariant()}.webp", item.ImagenUrl!, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Post_WhenCatalogItemProvided_UsesCatalogMetadataAndCurrentHousehold()
    {
        var user = await RegisterAsync("equip-catalog-create");
        var catalogoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.ElectrodomesticosCatalogo.Add(new ElectrodomesticoCatalogo
            {
                Id = catalogoId,
                Nombre = "Licuadora catalogo",
                Tipo = "Preparacion",
                Icono = "blender",
                ImagenUrl = "catalog/licuadora.webp",
                Orden = 1,
                Activo = true
            });
            await db.SaveChangesAsync();
        }

        Authenticate(user);
        var response = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            hogarId = user.HogarId,
            catalogoId,
            nombre = "Nombre cliente",
            tipo = "Tipo cliente",
            estado = "Activo",
            marca = "Marca cliente",
            imagenUrl = "https://malicious.example/electro.png"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ElectrodomesticoBody>();
        Assert.NotNull(body);
        Assert.Equal(user.HogarId, body!.HogarId);
        Assert.Equal(catalogoId, body.CatalogoId);
        Assert.Equal("Licuadora catalogo", body.Nombre);
        Assert.Equal("Preparacion", body.Tipo);
        Assert.Equal("Activo", body.Estado);
        Assert.Equal("Marca cliente", body.Marca);
        Assert.NotNull(body.ImagenUrl);
        Assert.StartsWith("https://", body.ImagenUrl!, StringComparison.Ordinal);
        Assert.EndsWith("catalog/licuadora.webp", body.ImagenUrl!, StringComparison.Ordinal);

        using var verificationScope = _factory.Services.CreateScope();
        var dbVerification = verificationScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var saved = await dbVerification.Electrodomesticos
            .Where(x => x.Id == body.Id)
            .Select(x => new { x.HogarId, x.CatalogoId, x.Nombre, x.Tipo })
            .SingleAsync();

        Assert.Equal(user.HogarId, saved.HogarId);
        Assert.Equal(catalogoId, saved.CatalogoId);
        Assert.Equal("Licuadora catalogo", saved.Nombre);
        Assert.Equal("Preparacion", saved.Tipo);
    }

    [Fact]
    public async Task Post_WhenCatalogItemIsInactive_ReturnsNotFound()
    {
        var user = await RegisterAsync("equip-catalog-inactive");
        var inactiveCatalogId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.ElectrodomesticosCatalogo.Add(new ElectrodomesticoCatalogo
            {
                Id = inactiveCatalogId,
                Nombre = "Catalog Inactive",
                Tipo = "Kitchen",
                Orden = 99,
                Activo = false
            });
            await db.SaveChangesAsync();
        }

        Authenticate(user);
        var response = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            catalogoId = inactiveCatalogId,
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_WhenElectrodomesticoExists_UpdatesTipoAndEstado()
    {
        var user = await RegisterAsync("equip-patch");

        Authenticate(user);
        var created = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            nombre = "Heladera vieja",
            tipo = "Cocina",
            estado = "Activo"
        });
        var electrodomestico = await created.Content.ReadFromJsonAsync<ElectrodomesticoBody>();
        Assert.NotNull(electrodomestico);

        var response = await _client.PatchAsJsonAsync($"/api/electrodomesticos/{electrodomestico!.Id}", new
        {
            tipo = "Lavadero",
            estado = "Fuera de servicio"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ElectrodomesticoBody>();
        Assert.NotNull(body);
        Assert.Equal("Lavadero", body!.Tipo);
        Assert.Equal("Fuera de servicio", body.Estado);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var saved = await db.Electrodomesticos.SingleAsync(x => x.Id == electrodomestico.Id);
        Assert.Equal("Lavadero", saved.Tipo);
        Assert.Equal("Fuera de servicio", saved.Estado);
    }

    [Fact]
    public async Task Patch_WhenNotFound_Returns404()
    {
        var user = await RegisterAsync("equip-patch-404");

        Authenticate(user);
        var response = await _client.PatchAsJsonAsync($"/api/electrodomesticos/{Guid.NewGuid()}", new
        {
            tipo = "Lavadero",
            estado = "Activo"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenElectrodomesticoExists_Returns204AndRemovesIt()
    {
        var user = await RegisterAsync("equip-delete");

        Authenticate(user);
        var created = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            nombre = "Microondas",
            tipo = "Cocina",
            estado = "Activo"
        });
        var electrodomestico = await created.Content.ReadFromJsonAsync<ElectrodomesticoBody>();
        Assert.NotNull(electrodomestico);

        var deleteResponse = await _client.DeleteAsync($"/api/electrodomesticos/{electrodomestico!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/electrodomesticos");
        var items = await listResponse.Content.ReadFromJsonAsync<List<ElectrodomesticoBody>>();
        Assert.DoesNotContain(items!, item => item.Id == electrodomestico.Id);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var user = await RegisterAsync("equip-delete-404");

        Authenticate(user);
        var response = await _client.DeleteAsync($"/api/electrodomesticos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenBelongsToAnotherHousehold_ReturnsNotFound()
    {
        var userA = await RegisterAsync("equip-delete-cross-a");
        var userB = await RegisterAsync("equip-delete-cross-b");

        Authenticate(userA);
        var created = await _client.PostAsJsonAsync("/api/electrodomesticos", new
        {
            nombre = "Cafetera",
            tipo = "Cocina",
            estado = "Activo"
        });
        var electrodomestico = await created.Content.ReadFromJsonAsync<ElectrodomesticoBody>();
        Assert.NotNull(electrodomestico);

        Authenticate(userB);
        var response = await _client.DeleteAsync($"/api/electrodomesticos/{electrodomestico!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<RegisterBody> RegisterAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var response = await _client.PostAsync("/api/auth/register", registerContent);

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
        string? ImagenUrl,
        Guid? CatalogoId);

    private sealed record ElectrodomesticoCatalogBody(
        Guid Id,
        string Nombre,
        string Tipo,
        string? Icono,
        string? ImagenUrl,
        int Orden);
}
