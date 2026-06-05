using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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

    private async Task<RegisterBody> AuthenticateAsync()
    {
        var email = $"receta-{Guid.NewGuid():N}@test.com";
        using var req = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var res  = await _client.PostAsync("/api/auth/register", req);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return body;
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
        Assert.False(await verifyDb.StockHogars.AnyAsync(s => s.Id == arvejasStockId));
        Assert.False(await verifyDb.StockHogars.AnyAsync(s => s.Id == pasasStockId));
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record RecetaBody(Guid Id, string Nombre, int VecesCocinada);
    private sealed record RecetaConIngredientesBody(Guid Id, string Nombre, List<IngredienteBody> Ingredientes);
    private sealed record IngredienteBody(Guid Id, List<string> Alergenos);
    private sealed record CocinarBody(Guid RecetaId, int VecesCocinada);
}
