using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Xunit;

namespace Nido.Api.IntegrationTests.Notificaciones;

public sealed class NotificacionesEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public NotificacionesEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StockBajoYVencimiento_GeneraYAlteraNotificaciones()
    {
        // 1. Registrar y autenticar usuario
        var email = $"notif-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var regBody = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regBody!.AccessToken);

        // 2. Traer notificaciones vacías al inicio (o solo onboarding si las hay)
        var initResponse = await _client.GetAsync("/api/notificaciones");
        Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);
        var initList = await initResponse.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        Assert.NotNull(initList);

        // 3. Crear un producto en stock que tenga Stock Bajo (PorcentajeConsumido = 80, CantidadEnvases = 1)
        var lowStockResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Crema de Leche",
            codigoBarras = "779123",
            imagen = "https://img.test/crema.png",
            ubicacion = "Heladera",
            cantidad = 1,
            fechaVencimiento = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"), // No vencido todavía
            estaAbierto = true,
            porcentajeConsumido = 80
        });
        Assert.Equal(HttpStatusCode.Created, lowStockResponse.StatusCode);
        var lowStockProd = await lowStockResponse.Content.ReadFromJsonAsync<StockItemBody>();

        // 4. Crear un producto que esté vencido (FechaVencimiento en el pasado)
        var expiredResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Yogur Vencido",
            codigoBarras = "779456",
            imagen = "https://img.test/yogur.png",
            ubicacion = "Heladera",
            cantidad = 1,
            fechaVencimiento = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"),
            estaAbierto = false,
            porcentajeConsumido = 0
        });
        Assert.Equal(HttpStatusCode.Created, expiredResponse.StatusCode);
        var expiredProd = await expiredResponse.Content.ReadFromJsonAsync<StockItemBody>();

        // 5. Traer notificaciones: Debe haber stock_bajo y producto_vencido
        var notifResponse = await _client.GetAsync("/api/notificaciones");
        Assert.Equal(HttpStatusCode.OK, notifResponse.StatusCode);
        var notifList = await notifResponse.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        Assert.NotNull(notifList);

        var lowStockNotif = notifList!.Find(n => n.Tipo == "stock_bajo" && n.ReferenciaId == lowStockProd!.Id);
        var expiredNotif = notifList!.Find(n => n.Tipo == "producto_vencido" && n.ReferenciaId == expiredProd!.Id);

        Assert.NotNull(lowStockNotif);
        Assert.Equal("alacena", lowStockNotif.ReferenciaTipo);
        Assert.Contains("Crema de Leche", lowStockNotif.Mensaje);

        Assert.NotNull(expiredNotif);
        Assert.Equal("alacena", expiredNotif.ReferenciaTipo);
        Assert.Contains("Yogur Vencido", expiredNotif.Mensaje);

        // 6. Actualizar el producto de stock bajo para que ya no lo esté (porcentajeConsumido = 20)
        var patchResponse = await _client.PatchAsJsonAsync($"/api/alacena/productos/{lowStockProd!.Id}", new
        {
            nombre = "Crema de Leche",
            cantidad = 1,
            ubicacion = "Heladera",
            unidadMedida = "unidad",
            fechaVencimiento = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"),
            estaAbierto = true,
            porcentajeConsumido = 20
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // 7. Traer notificaciones de nuevo: La de stock bajo debe haberse limpiado automáticamente
        var notifResponse2 = await _client.GetAsync("/api/notificaciones");
        var notifList2 = await notifResponse2.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        Assert.NotNull(notifList2);

        var lowStockNotif2 = notifList2!.Find(n => n.Tipo == "stock_bajo" && n.ReferenciaId == lowStockProd.Id);
        var expiredNotif2 = notifList2!.Find(n => n.Tipo == "producto_vencido" && n.ReferenciaId == expiredProd!.Id);

        Assert.Null(lowStockNotif2); // Limpiada
        Assert.NotNull(expiredNotif2); // Aún existe

        // 8. Eliminar el producto vencido
        var deleteResponse = await _client.DeleteAsync($"/api/alacena/productos/{expiredProd!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 9. Traer notificaciones de nuevo: La de producto vencido debe haberse limpiado
        var notifResponse3 = await _client.GetAsync("/api/notificaciones");
        var notifList3 = await notifResponse3.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        Assert.NotNull(notifList3);

        var expiredNotif3 = notifList3!.Find(n => n.Tipo == "producto_vencido" && n.ReferenciaId == expiredProd.Id);
        Assert.Null(expiredNotif3); // Limpiada
    }

    [Fact]
    public async Task ProductoPorAgotarse_SeGeneraNoSeDuplicaYSeSuprimeConStockBajo()
    {
        var email = $"notif-agotarse-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var regBody = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regBody!.AccessToken);

        var cafeResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Café",
            ubicacion = "Alacena",
            cantidad = 1,
            estaAbierto = false,
            porcentajeConsumido = 0
        });
        Assert.Equal(HttpStatusCode.Created, cafeResponse.StatusCode);
        var cafeProd = await cafeResponse.Content.ReadFromJsonAsync<StockItemBody>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

            var cafeStock = await db.StockHogars.SingleAsync(s => s.Id == cafeProd!.Id);
            cafeStock.CreatedAt = DateTime.UtcNow.AddDays(-8);

            db.StockHogars.Add(new StockHogar
            {
                Id = Guid.NewGuid(),
                HogarId = regBody.HogarId,
                ProductoId = cafeProd!.ProductoId,
                CargadoPor = regBody.UsuarioId,
                UpdatedBy = regBody.UsuarioId,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                Ubicacion = "Alacena",
                CantidadActual = 0,
                EstaAbierto = false,
                PorcentajeConsumido = 100
            });

            db.ConsumosProducto.Add(new ConsumoProducto
            {
                Id = Guid.NewGuid(),
                HogarId = regBody.HogarId,
                ProductoId = cafeProd.ProductoId,
                ProductoNombre = "Café",
                Cantidad = 1,
                UnidadMedida = "unidad",
                Motivo = "Cocinado",
                FechaConsumo = DateTime.UtcNow.AddDays(-3),
                UsuarioId = regBody.UsuarioId
            });

            await db.SaveChangesAsync();
        }

        var notifResponse = await _client.GetAsync("/api/notificaciones");
        Assert.Equal(HttpStatusCode.OK, notifResponse.StatusCode);
        var notifList = await notifResponse.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        var agotarseNotif = notifList!.Find(n => n.Tipo == "producto_por_agotarse" && n.ReferenciaId == cafeProd!.Id);

        Assert.NotNull(agotarseNotif);
        Assert.Equal("alacena", agotarseNotif.ReferenciaTipo);
        Assert.Contains("Café", agotarseNotif.Mensaje);

        var patchUbicacion = await _client.PatchAsJsonAsync($"/api/alacena/productos/{cafeProd!.Id}", new
        {
            nombre = "Café",
            cantidad = 1,
            ubicacion = "Heladera",
            estaAbierto = false,
            porcentajeConsumido = 0
        });
        Assert.Equal(HttpStatusCode.OK, patchUbicacion.StatusCode);

        var notifResponse2 = await _client.GetAsync("/api/notificaciones");
        var notifList2 = await notifResponse2.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        var agotarseNotifs2 = notifList2!.FindAll(n => n.Tipo == "producto_por_agotarse" && n.ReferenciaId == cafeProd.Id);
        Assert.Single(agotarseNotifs2);

        var patchStockBajo = await _client.PatchAsJsonAsync($"/api/alacena/productos/{cafeProd.Id}", new
        {
            nombre = "Café",
            cantidad = 1,
            ubicacion = "Heladera",
            estaAbierto = true,
            porcentajeConsumido = 80
        });
        Assert.Equal(HttpStatusCode.OK, patchStockBajo.StatusCode);

        var notifResponse3 = await _client.GetAsync("/api/notificaciones");
        var notifList3 = await notifResponse3.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        var stockBajoNotif = notifList3!.Find(n => n.Tipo == "stock_bajo" && n.ReferenciaId == cafeProd.Id);
        var agotarseNotif3 = notifList3!.Find(n => n.Tipo == "producto_por_agotarse" && n.ReferenciaId == cafeProd.Id);

        Assert.NotNull(stockBajoNotif);
        Assert.Null(agotarseNotif3);

        var patchRevertir = await _client.PatchAsJsonAsync($"/api/alacena/productos/{cafeProd.Id}", new
        {
            nombre = "Café",
            cantidad = 1,
            ubicacion = "Heladera",
            estaAbierto = false,
            porcentajeConsumido = 0
        });
        Assert.Equal(HttpStatusCode.OK, patchRevertir.StatusCode);

        var notifResponse4 = await _client.GetAsync("/api/notificaciones");
        var notifList4 = await notifResponse4.Content.ReadFromJsonAsync<List<NotificacionBody>>();
        var agotarseNotif4 = notifList4!.Find(n => n.Tipo == "producto_por_agotarse" && n.ReferenciaId == cafeProd.Id);

        Assert.NotNull(agotarseNotif4);
    }

    [Fact]
    public async Task SubscribePush_WithValidPayload_ReturnsNoContent_AndKeepsSingleSubscriptionPerEndpoint()
    {
        await RegisterAndAuthenticateAsync();

        // Use a realistic FCM endpoint format so it passes the SSRF allowlist.
        var request = new
        {
            endpoint = "https://fcm.googleapis.com/fcm/send/subscriptions/test-token-abc123",
            p256dh = "key-a",
            auth = "auth-a"
        };

        var first = await _client.PostAsJsonAsync("/api/notificaciones/suscripciones", request);
        var second = await _client.PostAsJsonAsync("/api/notificaciones/suscripciones", new
        {
            endpoint = request.endpoint,
            p256dh = "key-b",
            auth = "auth-b"
        });

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var subscriptions = await db.SuscripcionesPush
            .AsNoTracking()
            .Where(x => x.Endpoint == request.endpoint)
            .ToListAsync();

        var subscription = Assert.Single(subscriptions);
        Assert.Equal("key-b", subscription.P256dh);
        Assert.Equal("auth-b", subscription.Auth);
    }

    [Fact]
    public async Task SubscribePush_WithMissingFields_ReturnsBadRequest()
    {
        await RegisterAndAuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/notificaciones/suscripciones", new
        {
            endpoint = "   ",
            p256dh = "key-a",
            auth = "auth-a"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task RegisterAndAuthenticateAsync()
    {
        var email = $"notif-subscribe-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var regBody = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", regBody!.AccessToken);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record StockItemBody(Guid Id, Guid ProductoId, string Nombre, string? Imagen, string? CodigoBarras, string Ubicacion, decimal Cantidad, string? UnidadMedida, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
    private sealed record NotificacionBody(Guid Id, Guid UsuarioId, string? Tipo, string? Mensaje, bool Leida, Guid? ReferenciaId, string? ReferenciaTipo, DateTime CreatedAt);
}
