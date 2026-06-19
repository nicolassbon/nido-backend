using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Planificador;

public sealed class PlanificadorEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public PlanificadorEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddItem_WhenTask_ReturnsItemAndAppearsInWeek()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-task");

        var response = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Limpiar la heladera",
            hora = "10:30"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.NotNull(created);
        Assert.Equal("tarea", created!.TipoComida);
        Assert.Equal("Limpiar la heladera", created.TituloLibre);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        Assert.Equal(HttpStatusCode.OK, weekResponse.StatusCode);
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal(created.Id, item.Id);
        Assert.Equal("Limpiar la heladera", item.TituloLibre);
    }

    [Fact]
    public async Task AddItem_WhenRecipe_ReturnsItemAndAppearsInWeek()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-meal");
        var recetaId = await SeedRecipeAsync("Tarta de verduras");

        var response = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "almuerzo",
            recetaId,
            tituloLibre = (string?)null,
            hora = "13:00"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.NotNull(created);
        Assert.Equal("almuerzo", created!.TipoComida);
        Assert.Equal(recetaId, created.RecetaId);
        Assert.Equal("Tarta de verduras", created.RecetaNombre);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        Assert.Equal(HttpStatusCode.OK, weekResponse.StatusCode);
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal(created.Id, item.Id);
        Assert.Equal("Tarta de verduras", item.RecetaNombre);
    }

    private async Task<Guid> SeedRecipeAsync(string nombre)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var receta = new Receta
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Descripcion = "Receta de prueba",
            TiempoCoccionMin = 30,
            Dificultad = "Facil",
            Porciones = 4
        };

        db.Recetas.Add(receta);
        await db.SaveChangesAsync();
        return receta.Id;
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

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record PlanificadorSemanaBody(Guid Id, string FechaInicio, List<PlanificadorItemBody> Items);
    private sealed record PlanificadorItemBody(
        Guid Id,
        string Fecha,
        string TipoComida,
        Guid? RecetaId,
        string? RecetaNombre,
        string? ImagenUrl,
        string? TituloLibre,
        string? Hora,
        int Orden,
        Guid CreadoPor);
}
