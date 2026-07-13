using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    private async Task<RegisterBody> AuthenticateAsync()
        => await AuthenticateAsync(_client);

    private static async Task<RegisterBody> AuthenticateAsync(HttpClient client)
    {
        var email = $"receta-{Guid.NewGuid():N}@test.com";
        using var req = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var res  = await client.PostAsync("/api/auth/register", req);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return body;
    }

    private static async Task MakePremiumAsync(WebApplicationFactory<Program> factory, Guid hogarId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var hogar = await db.Hogares.SingleAsync(x => x.Id == hogarId);
        hogar.Plan = "premium";
        hogar.SubscriptionStatus = "active";
        hogar.SuscripcionVenceEl = DateTime.UtcNow.AddDays(30);
        hogar.PlanUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedRecetaAsync(string nombre = "Arroz blanco")
    {
        return await SeedRecetaAsync(_factory, nombre);
    }

    private static async Task<Guid> SeedRecetaAsync(WebApplicationFactory<Program> factory, string nombre = "Arroz blanco")
    {
        var id = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
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

    private async Task<Guid> SeedRecipeWithExpiringStockAsync(
        RegisterBody auth,
        string recetaNombre,
        string productoNombre,
        string ingredienteNombre,
        DateOnly fechaVencimiento,
        int diasAlerta = 3,
        bool ingredienteConProductoId = true)
    {
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var usuario = await db.Usuarios.SingleAsync(usuario => usuario.Id == auth.UsuarioId);
        usuario.AlertaVencimientoDias = diasAlerta;

        db.Productos.Add(new Producto
        {
            Id = productoId,
            Nombre = productoNombre
        });
        db.Recetas.Add(new Receta
        {
            Id = recetaId,
            Nombre = recetaNombre,
            Descripcion = "Receta de prueba",
            Dificultad = "Facil",
            Porciones = 2,
            TiempoCoccionMin = 20,
        });
        db.IngredientesReceta.Add(new IngredientesRecetum
        {
            Id = Guid.NewGuid(),
            RecetaId = recetaId,
            ProductoId = ingredienteConProductoId ? productoId : null,
            NombreIngrediente = ingredienteNombre,
            Cantidad = 1,
            Unidad = "unidad"
        });
        db.StockHogars.Add(new StockHogar
        {
            Id = Guid.NewGuid(),
            HogarId = auth.HogarId,
            ProductoId = productoId,
            CargadoPor = auth.UsuarioId,
            UpdatedBy = auth.UsuarioId,
            CantidadActual = 1m,
            UnidadMedida = "unidad",
            FechaVencimiento = fechaVencimiento,
            Ubicacion = "Alacena",
            EstaAbierto = false,
            PorcentajeConsumido = 0m
        });

        await db.SaveChangesAsync();
        return recetaId;
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
    public async Task AsistentePorIa_CuandoHogarEsFree_Returns403YNoInvocaClienteIa()
    {
        StubHttpClientFactory? iaClientFactory = null;
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();
            iaClientFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("IA client should not be used for free households."));
            services.AddSingleton<IHttpClientFactory>(iaClientFactory);
        }));
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/recetas/ia/asistente", new { pregunta = "¿Qué puedo cocinar?" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("PLAN_UPGRADE_REQUIRED", body!.Title);
        Assert.NotNull(iaClientFactory);
        Assert.DoesNotContain(iaClientFactory!.RequestedUris, uri => uri.Contains("/api/ia/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AsistentePorIa_CuandoHogarEsPremium_ReturnsAssistantResponse()
    {
        using var factory = WithIaClient(_ => JsonResponse("""{"respuesta":"Podés cocinar una tortilla rápida."}"""));
        using var client = factory.CreateClient();
        var auth = await AuthenticateAsync(client);
        await MakePremiumAsync(factory, auth.HogarId);

        var response = await client.PostAsJsonAsync("/api/recetas/ia/asistente", new { pregunta = "¿Qué puedo cocinar?" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AsistenteIaBody>();
        Assert.NotNull(body);
        Assert.Equal("Podés cocinar una tortilla rápida.", body!.Respuesta);
    }

    [Fact]
    public async Task RecomendarPorIa_CuandoHogarEsFree_Returns403YNoInvocaClienteIa()
    {
        StubHttpClientFactory? iaClientFactory = null;
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();
            iaClientFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("IA client should not be used for free households."));
            services.AddSingleton<IHttpClientFactory>(iaClientFactory);
        }));
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/recetas/ia/recomendar?busqueda=pollo&objetivo=alta-proteina");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("PLAN_UPGRADE_REQUIRED", body!.Title);
        Assert.NotNull(iaClientFactory);
        Assert.DoesNotContain(iaClientFactory!.RequestedUris, uri => uri.Contains("/api/ia/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RecomendarPorIa_CuandoHogarEsPremium_ReturnsMatchedRecipes()
    {
        using var factory = WithIaClient(_ => JsonResponse("""{"recetas":[{"nombre":"Tarta de ricota"}]}"""));
        using var client = factory.CreateClient();
        var auth = await AuthenticateAsync(client);
        await MakePremiumAsync(factory, auth.HogarId);
        var recetaId = await SeedRecetaAsync(factory, "Tarta de ricota");

        var response = await client.GetAsync("/api/recetas/ia/recomendar?busqueda=ricota");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        Assert.NotNull(body);
        var receta = Assert.Single(body!);
        Assert.Equal(recetaId, receta.Id);
        Assert.Equal("Tarta de ricota", receta.Nombre);
    }

    [Fact]
    public async Task GetAll_CuandoProductoVenceDentroDeAlerta_MarcaRecetaUrgente()
    {
        var auth = await AuthenticateAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var fechaVencimiento = hoy.AddDays(2);
        var recetaId = await SeedRecipeWithExpiringStockAsync(
            auth,
            "Tarta urgente",
            "Ricota",
            "Ricota",
            fechaVencimiento);

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.True(receta.TieneProductosPorVencer);
        Assert.Equal(fechaVencimiento.ToString("yyyy-MM-dd"), receta.FechaVencimientoMasProxima);
        Assert.Equal(2, receta.DiasHastaVencimiento);
        var producto = Assert.Single(receta.ProductosPorVencer);
        Assert.Equal("Ricota", producto.Nombre);
        Assert.Equal(fechaVencimiento.ToString("yyyy-MM-dd"), producto.FechaVencimiento);
        Assert.Equal(2, producto.DiasHastaVencimiento);
    }

    [Fact]
    public async Task GetAll_CuandoProductoVenceDespuesDeAlerta_NoMarcaRecetaUrgente()
    {
        var auth = await AuthenticateAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var recetaId = await SeedRecipeWithExpiringStockAsync(
            auth,
            "Salsa sin urgencia",
            "Tomate",
            "Tomate",
            hoy.AddDays(4));

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.False(receta.TieneProductosPorVencer);
        Assert.Null(receta.FechaVencimientoMasProxima);
        Assert.Null(receta.DiasHastaVencimiento);
    }

    [Fact]
    public async Task GetAll_CuandoProductoVencioAyer_NoMarcaRecetaUrgente()
    {
        var auth = await AuthenticateAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var recetaId = await SeedRecipeWithExpiringStockAsync(
            auth,
            "Budin vencido",
            "Leche",
            "Leche",
            hoy.AddDays(-1));

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.False(receta.TieneProductosPorVencer);
        Assert.Null(receta.FechaVencimientoMasProxima);
        Assert.Null(receta.DiasHastaVencimiento);
    }

    [Fact]
    public async Task GetAll_CuandoIngredienteSinProductoIdMatcheaPorNombre_MarcaRecetaUrgente()
    {
        var auth = await AuthenticateAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var recetaId = await SeedRecipeWithExpiringStockAsync(
            auth,
            "Arroz por nombre",
            "Arroz integral",
            "Arroz integral",
            hoy.AddDays(1),
            ingredienteConProductoId: false);

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.True(receta.TieneProductosPorVencer);
        Assert.Equal(1, receta.DiasHastaVencimiento);
    }

    [Fact]
    public async Task GetAll_UsaFechaMasProximaEntreProductosUrgentes()
    {
        var auth = await AuthenticateAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var recetaId = Guid.NewGuid();
        var lecheId = Guid.NewGuid();
        var harinaId = Guid.NewGuid();
        var fechaMasProxima = hoy.AddDays(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var usuario = await db.Usuarios.SingleAsync(usuario => usuario.Id == auth.UsuarioId);
            usuario.AlertaVencimientoDias = 7;
            db.Productos.AddRange(
                new Producto { Id = lecheId, Nombre = "Leche" },
                new Producto { Id = harinaId, Nombre = "Harina" });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Panqueques urgentes",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.AddRange(
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = lecheId, NombreIngrediente = "Leche", Cantidad = 1, Unidad = "lt" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = harinaId, NombreIngrediente = "Harina", Cantidad = 500, Unidad = "g" });
            db.StockHogars.AddRange(
                new StockHogar
                {
                    Id = Guid.NewGuid(),
                    HogarId = auth.HogarId,
                    ProductoId = lecheId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 1m,
                    UnidadMedida = "lt",
                    FechaVencimiento = hoy.AddDays(5),
                    Ubicacion = "Heladera",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                },
                new StockHogar
                {
                    Id = Guid.NewGuid(),
                    HogarId = auth.HogarId,
                    ProductoId = harinaId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 1m,
                    UnidadMedida = "kg",
                    FechaVencimiento = fechaMasProxima,
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.True(receta.TieneProductosPorVencer);
        Assert.Equal(fechaMasProxima.ToString("yyyy-MM-dd"), receta.FechaVencimientoMasProxima);
        Assert.Equal(1, receta.DiasHastaVencimiento);
        Assert.Equal(["Harina", "Leche"], receta.ProductosPorVencer.Select(producto => producto.Nombre).ToArray());
    }

    [Fact]
    public async Task GetAll_CuandoIngredienteMatcheaAlergeno_DevuelveAlergenos()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Harina de trigo"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Pan casero",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Harina de trigo",
                Cantidad = 200,
                Unidad = "g"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaConIngredientesBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.Contains("Gluten", receta.Ingredientes.Single().Alergenos);
    }

    [Fact]
    public async Task GetAll_CuandoIngredienteTieneLactosaOCarne_DevuelveAlergenos()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var ricotaId = Guid.NewGuid();
        var polloId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.AddRange(
                new Producto
                {
                    Id = ricotaId,
                    Nombre = "Ricota"
                },
                new Producto
                {
                    Id = polloId,
                    Nombre = "Pechuga de pollo"
                });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Tarta de pollo y ricota",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.AddRange(
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = ricotaId,
                    NombreIngrediente = "Ricota",
                    Cantidad = 200,
                    Unidad = "g"
                },
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = polloId,
                    NombreIngrediente = "Pechuga de pollo",
                    Cantidad = 1,
                    Unidad = "unidad"
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/recetas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = await response.Content.ReadFromJsonAsync<List<RecetaConIngredientesBody>>();
        var receta = lista!.Single(r => r.Id == recetaId);
        Assert.Contains(receta.Ingredientes, ingrediente => ingrediente.Alergenos.Contains("Lactosa"));
        Assert.Contains(receta.Ingredientes, ingrediente => ingrediente.Alergenos.Contains("Carne"));
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
    public async Task GetById_SinAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/recetas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_Returns404()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/recetas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_CuandoIngredienteTieneCompraEstandar_DevuelveElEstandarDeCompra()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Arroz",
                CantidadCompraEstandar = 1m,
                UnidadCompraEstandar = "kg"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Arroz salteado",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Arroz",
                Cantidad = 200m,
                Unidad = "g"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaDetalleCompraBody>();
        var ingrediente = Assert.Single(body!.Ingredientes);
        Assert.Equal(1m, ingrediente.CantidadCompraEstandar);
        Assert.Equal("kg", ingrediente.UnidadCompraEstandar);
    }

    [Fact]
    public async Task GetById_CuandoIngredienteNoTieneCompraEstandar_DevuelveNull()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Salsa casera",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = null,
                NombreIngrediente = "Ingrediente imposible xyz",
                Cantidad = 1m,
                Unidad = "pizca"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaDetalleCompraBody>();
        var ingrediente = Assert.Single(body!.Ingredientes);
        Assert.Null(ingrediente.CantidadCompraEstandar);
        Assert.Null(ingrediente.UnidadCompraEstandar);
    }

    [Fact]
    public async Task GetById_DevuelveCantidadYUnidadParaListaDeCompras()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Panqueques",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.AddRange(
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = null,
                    NombreIngrediente = "Agua",
                    Cantidad = 2m,
                    Unidad = "taza"
                },
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = null,
                    NombreIngrediente = "Harina",
                    Cantidad = 1m,
                    Unidad = "taza"
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaDetalleCompraBody>();
        var agua = body!.Ingredientes.Single(i => i.Nombre == "Agua");
        var harina = body.Ingredientes.Single(i => i.Nombre == "Harina");
        Assert.Equal(480m, agua.CantidadListaCompras);
        Assert.Equal("ml", agua.UnidadListaCompras);
        Assert.Equal(120m, harina.CantidadListaCompras);
        Assert.Equal("g", harina.UnidadListaCompras);
    }

    [Fact]
    public async Task GetById_CuandoIngredienteNoTieneProductoId_PeroMatcheaPorNombre_DevuelveCompraEstandar()
    {
        await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Aceite de oliva",
                CantidadCompraEstandar = 1m,
                UnidadCompraEstandar = "lt"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Arroz salteado",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = null,
                NombreIngrediente = "Aceite de oliva",
                Cantidad = 1m,
                Unidad = "cda"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaDetalleCompraBody>();
        var ingrediente = Assert.Single(body!.Ingredientes);
        Assert.Equal(1m, ingrediente.CantidadCompraEstandar);
        Assert.Equal("lt", ingrediente.UnidadCompraEstandar);
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

    [Fact]
    public async Task Cocinar_ConvierteUnidadIngrediente_AUnidadDelStock()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Harina"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Pan",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Harina",
                Cantidad = 500m,
                Unidad = "g"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "kg",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(0.5m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_ConvierteUnidadGenerica_AVolumenDelStock()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Aceite"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Ensalada",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Aceite",
                Cantidad = 2m,
                Unidad = "unidad"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "lt",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(0.8m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_DescuentaUnidadConCantidadEmbebida()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Leche"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Panqueques",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Leche",
                Cantidad = 1m,
                Unidad = "1 taza"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 500m,
                UnidadMedida = "ml",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(260m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_DescuentaIngredienteConCantidadNulaYUnidadConFraccion()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Caldo"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Sopa",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Caldo",
                Cantidad = null,
                Unidad = "1/2 taza"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 1m,
                UnidadMedida = "lt",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(0.88m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_DescuentaMedidasDeReferenciaEnVolumen()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var lecheId = Guid.NewGuid();
        var aceiteId = Guid.NewGuid();
        var extractoId = Guid.NewGuid();
        var lecheStockId = Guid.NewGuid();
        var aceiteStockId = Guid.NewGuid();
        var extractoStockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.AddRange(
                new Producto { Id = lecheId, Nombre = "Leche" },
                new Producto { Id = aceiteId, Nombre = "Aceite" },
                new Producto { Id = extractoId, Nombre = "Extracto" });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Medidas caseras",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.AddRange(
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = lecheId,
                    NombreIngrediente = "Leche",
                    Cantidad = 2m,
                    Unidad = "vaso"
                },
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = aceiteId,
                    NombreIngrediente = "Aceite",
                    Cantidad = null,
                    Unidad = "\u00BD cucharadita"
                },
                new IngredientesRecetum
                {
                    Id = Guid.NewGuid(),
                    RecetaId = recetaId,
                    ProductoId = extractoId,
                    NombreIngrediente = "Extracto",
                    Cantidad = 1m,
                    Unidad = "pizca"
                });
            db.StockHogars.AddRange(
                new StockHogar
                {
                    Id = lecheStockId,
                    HogarId = auth.HogarId,
                    ProductoId = lecheId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 1m,
                    UnidadMedida = "lt",
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                },
                new StockHogar
                {
                    Id = aceiteStockId,
                    HogarId = auth.HogarId,
                    ProductoId = aceiteId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 100m,
                    UnidadMedida = "ml",
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                },
                new StockHogar
                {
                    Id = extractoStockId,
                    HogarId = auth.HogarId,
                    ProductoId = extractoId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 10m,
                    UnidadMedida = "ml",
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var lecheStock = await verifyDb.StockHogars.SingleAsync(s => s.Id == lecheStockId);
        var aceiteStock = await verifyDb.StockHogars.SingleAsync(s => s.Id == aceiteStockId);
        var extractoStock = await verifyDb.StockHogars.SingleAsync(s => s.Id == extractoStockId);
        Assert.Equal(0.5m, lecheStock.CantidadActual);
        Assert.Equal(97.5m, aceiteStock.CantidadActual);
        Assert.Equal(9.7m, extractoStock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_ConvierteTazaDeIngredienteSeco_AGramosDelStock()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Harina"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Bizcochuelo",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Harina",
                Cantidad = 1m,
                Unidad = "taza"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 500m,
                UnidadMedida = "g",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(380m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_DescuentaIngredienteSinProductoId_MatcheandoPorNombre()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Harina"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Pan sin producto_id",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = null,
                NombreIngrediente = "Harina comun",
                Cantidad = 1m,
                Unidad = "taza"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 500m,
                UnidadMedida = "g",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(380m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_UsaConversionGenericaCuandoNoConoceDensidad()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Condimento italiano"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Salsa generica",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = null,
                NombreIngrediente = "Condimento italiano",
                Cantidad = 1m,
                Unidad = "taza"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 500m,
                UnidadMedida = "g",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(400m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_ArrozPakistani_DescuentaIngredientesPorNombreYMedidasMixtas()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var aguaId = Guid.NewGuid();
        var arrozId = Guid.NewGuid();
        var arvejasId = Guid.NewGuid();
        var cebollaId = Guid.NewGuid();
        var mantecaId = Guid.NewGuid();
        var pasasId = Guid.NewGuid();
        var salId = Guid.NewGuid();
        var aguaStockId = Guid.NewGuid();
        var arrozStockId = Guid.NewGuid();
        var arvejasStockId = Guid.NewGuid();
        var cebollaStockId = Guid.NewGuid();
        var mantecaStockId = Guid.NewGuid();
        var pasasStockId = Guid.NewGuid();
        var salStockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.AddRange(
                new Producto { Id = aguaId, Nombre = "Agua" },
                new Producto { Id = arrozId, Nombre = "Arroz" },
                new Producto { Id = arvejasId, Nombre = "Arvejas" },
                new Producto { Id = cebollaId, Nombre = "Cebolla" },
                new Producto { Id = mantecaId, Nombre = "Manteca" },
                new Producto { Id = pasasId, Nombre = "Pasas de uva" },
                new Producto { Id = salId, Nombre = "Sal" });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Arroz pakistani",
                Descripcion = "Receta de prueba",
                Dificultad = "Media",
                Porciones = 4,
                TiempoCoccionMin = 45,
            });
            db.IngredientesReceta.AddRange(
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Agua", Cantidad = 2m, Unidad = "taza" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Arroz precocido", Cantidad = 2m, Unidad = "taza" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Arvejas", Cantidad = 0.5m, Unidad = "taza" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Cebolla grande", Cantidad = 0.5m, Unidad = "unidad" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Manteca", Cantidad = 3m, Unidad = "cda" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Pasas de uva", Cantidad = 1m, Unidad = "taza" },
                new IngredientesRecetum { Id = Guid.NewGuid(), RecetaId = recetaId, ProductoId = null, NombreIngrediente = "Sal", Cantidad = 1m, Unidad = "cdta" });
            db.StockHogars.AddRange(
                new StockHogar { Id = aguaStockId, HogarId = auth.HogarId, ProductoId = aguaId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 1m, UnidadMedida = "lt", Ubicacion = "Heladera", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = arrozStockId, HogarId = auth.HogarId, ProductoId = arrozId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 2m, UnidadMedida = "kg", Ubicacion = "Alacena", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = arvejasStockId, HogarId = auth.HogarId, ProductoId = arvejasId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 1m, UnidadMedida = "unidad", Ubicacion = "Alacena", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = cebollaStockId, HogarId = auth.HogarId, ProductoId = cebollaId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 1m, UnidadMedida = "kg", Ubicacion = "Alacena", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = mantecaStockId, HogarId = auth.HogarId, ProductoId = mantecaId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 1m, UnidadMedida = "unidad", Ubicacion = "Heladera", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = pasasStockId, HogarId = auth.HogarId, ProductoId = pasasId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 1m, UnidadMedida = "unidad", Ubicacion = "Alacena", EstaAbierto = false, PorcentajeConsumido = 0m },
                new StockHogar { Id = salStockId, HogarId = auth.HogarId, ProductoId = salId, CargadoPor = auth.UsuarioId, UpdatedBy = auth.UsuarioId, CantidadActual = 100m, UnidadMedida = "g", Ubicacion = "Alacena", EstaAbierto = false, PorcentajeConsumido = 0m });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.Equal(0.52m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == aguaStockId)).CantidadActual);
        Assert.Equal(1.60m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == arrozStockId)).CantidadActual);
        Assert.Equal(0.95m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == cebollaStockId)).CantidadActual);
        Assert.Equal(0.55m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == mantecaStockId)).CantidadActual);
        Assert.Equal(94m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == salStockId)).CantidadActual);
        Assert.Equal(0m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == arvejasStockId)).CantidadActual);
        Assert.Equal(0m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == pasasStockId)).CantidadActual);
    }

    [Fact]
    public async Task GetById_CuandoIngredienteTieneStockSoloEnOtroHogar_EnStockEsFalse()
    {
        var auth = await AuthenticateAsync();
        using var otherClient = _factory.CreateClient();
        var otherAuth = await AuthenticateAsync(otherClient);
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Tomate"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Salsa de tomate",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Tomate",
                Cantidad = 2m,
                Unidad = "unidad"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = Guid.NewGuid(),
                HogarId = otherAuth.HogarId,
                ProductoId = productoId,
                CargadoPor = otherAuth.UsuarioId,
                UpdatedBy = otherAuth.UsuarioId,
                CantidadActual = 5m,
                UnidadMedida = "unidad",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                Ubicacion = "Heladera",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/recetas/{recetaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecetaDetalleBody>();
        Assert.NotNull(body);
        Assert.Equal(recetaId, body!.Id);
        Assert.Single(body.Ingredientes);
        Assert.False(body.Ingredientes[0].EnStock);
    }

    [Fact]
    public async Task Cocinar_CuandoHayMultiplesStocksDelMismoProducto_ConsumePrimeroElQueVenceAntes()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockViejoId = Guid.NewGuid();
        var stockNuevoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Azucar"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Budin",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Azucar",
                Cantidad = 300m,
                Unidad = "g"
            });
            db.StockHogars.AddRange(
                new StockHogar
                {
                    Id = stockViejoId,
                    HogarId = auth.HogarId,
                    ProductoId = productoId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 200m,
                    UnidadMedida = "g",
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                },
                new StockHogar
                {
                    Id = stockNuevoId,
                    HogarId = auth.HogarId,
                    ProductoId = productoId,
                    CargadoPor = auth.UsuarioId,
                    UpdatedBy = auth.UsuarioId,
                    CantidadActual = 300m,
                    UnidadMedida = "g",
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0m
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.Equal(0m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == stockViejoId)).CantidadActual);
        var stockRestante = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockNuevoId);
        Assert.Equal(200m, stockRestante.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_CuandoUnidadNoEsConvertible_NoModificaStockPeroRegistraLaCoccion()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Perejil"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Salsa verde",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Perejil",
                Cantidad = 1m,
                Unidad = "atado"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 100m,
                UnidadMedida = "g",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Heladera",
                EstaAbierto = false,
                PorcentajeConsumido = 0m
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CocinarBody>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.VecesCocinada);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var stock = await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId);
        Assert.Equal(100m, stock.CantidadActual);
    }

    [Fact]
    public async Task Cocinar_CuandoNoHayStock_RegistraCoccionYNoModificaStock()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Leche"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Panqueques sin stock",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Leche",
                Cantidad = 1,
                Unidad = "lt"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CocinarBody>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.VecesCocinada);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.False(await verifyDb.StockHogars.AnyAsync(s => s.HogarId == auth.HogarId && s.ProductoId == productoId));
        Assert.Equal(1, await verifyDb.RecetasCocinadas.CountAsync(rc => rc.RecetaId == recetaId && rc.HogarId == auth.HogarId));
    }

    [Fact]
    public async Task Cocinar_CuandoStockEsInsuficiente_ConsumeTodoElStockDisponibleYRegistraCoccion()
    {
        var auth = await AuthenticateAsync();
        var recetaId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productoId,
                Nombre = "Harina"
            });
            db.Recetas.Add(new Receta
            {
                Id = recetaId,
                Nombre = "Pan casero",
                Descripcion = "Receta de prueba",
                Dificultad = "Facil",
                Porciones = 2,
                TiempoCoccionMin = 20,
            });
            db.IngredientesReceta.Add(new IngredientesRecetum
            {
                Id = Guid.NewGuid(),
                RecetaId = recetaId,
                ProductoId = productoId,
                NombreIngrediente = "Harina",
                Cantidad = 500,
                Unidad = "g"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = stockId,
                HogarId = auth.HogarId,
                ProductoId = productoId,
                CargadoPor = auth.UsuarioId,
                UpdatedBy = auth.UsuarioId,
                CantidadActual = 200,
                UnidadMedida = "g",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/recetas/{recetaId}/cocinar", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CocinarBody>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.VecesCocinada);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.Equal(0m, (await verifyDb.StockHogars.SingleAsync(s => s.Id == stockId)).CantidadActual);
        Assert.Equal(1, await verifyDb.RecetasCocinadas.CountAsync(rc => rc.RecetaId == recetaId && rc.HogarId == auth.HogarId));
    }

    private static WebApplicationFactory<Program> WithIaClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        new NidoTestWebAppFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(responseFactory));
        }));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record AsistenteIaBody(string Respuesta);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
    private sealed record RecetaBody(
        Guid Id,
        string Nombre,
        int VecesCocinada,
        bool TieneProductosPorVencer,
        string? FechaVencimientoMasProxima,
        int? DiasHastaVencimiento,
        List<ProductoPorVencerBody> ProductosPorVencer);
    private sealed record ProductoPorVencerBody(
        Guid ProductoId,
        string Nombre,
        string FechaVencimiento,
        int DiasHastaVencimiento);
    private sealed record RecetaDetalleBody(Guid Id, List<IngredienteDetalleBody> Ingredientes, int VecesCocinada);
    private sealed record RecetaDetalleCompraBody(Guid Id, List<IngredienteDetalleCompraBody> Ingredientes, int VecesCocinada);
    private sealed record RecetaConIngredientesBody(Guid Id, string Nombre, List<IngredienteBody> Ingredientes);
    private sealed record IngredienteBody(Guid Id, List<string> Alergenos);
    private sealed record IngredienteDetalleBody(Guid Id, bool EnStock, List<string> Alergenos);
    private sealed record IngredienteDetalleCompraBody(
        Guid Id,
        string Nombre,
        decimal? CantidadCompraEstandar,
        string? UnidadCompraEstandar,
        decimal? CantidadListaCompras,
        string? UnidadListaCompras,
        bool EnStock,
        List<string> Alergenos);
    private sealed record CocinarBody(Guid RecetaId, int VecesCocinada);

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int CreateClientCalls { get; private set; }
        public List<string> RequestedUris { get; } = [];

        public HttpClient CreateClient(string name)
        {
            CreateClientCalls++;
            return new HttpClient(new StubHttpMessageHandler(request =>
            {
                RequestedUris.Add(request.RequestUri?.ToString() ?? string.Empty);
                return _responseFactory(request);
            }))
            {
                BaseAddress = new Uri("http://localhost/")
            };
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
