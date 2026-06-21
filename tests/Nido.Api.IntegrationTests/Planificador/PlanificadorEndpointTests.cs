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
        var user = await RegisterAndAuthenticateAsync(_client, "plan-task");

        var response = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Limpiar la heladera",
            hora = "10:30",
            asignadoA = user.UsuarioId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.NotNull(created);
        Assert.Equal("tarea", created!.TipoComida);
        Assert.Equal("Limpiar la heladera", created.TituloLibre);
        Assert.NotNull(created.TareaId);
        Assert.Equal("pendiente", created.TareaEstado);
        Assert.Equal(user.UsuarioId, created.AsignadoA!.UsuarioId);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        Assert.Equal(HttpStatusCode.OK, weekResponse.StatusCode);
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal(created.Id, item.Id);
        Assert.Equal("Limpiar la heladera", item.TituloLibre);
        Assert.Equal(created.TareaId, item.TareaId);

        var tareasResponse = await _client.GetAsync("/api/tareas");
        var tareas = await tareasResponse.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.Contains(tareas!, tarea => tarea.Id == created.TareaId && tarea.Titulo == "Limpiar la heladera");
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

    [Fact]
    public async Task AddItem_WhenRecipeImageIsStorageKey_ReturnsPublicImageUrl()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-meal-image");
        var recetaId = await SeedRecipeAsync("Sopa de calabaza", "recipes/sopa.webp");

        var response = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "cena",
            recetaId,
            tituloLibre = (string?)null,
            hora = "20:00"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.StartsWith("https://", created!.ImagenUrl, StringComparison.Ordinal);
        Assert.EndsWith("/recipes/sopa.webp", created.ImagenUrl, StringComparison.Ordinal);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.StartsWith("https://", item.ImagenUrl, StringComparison.Ordinal);
        Assert.EndsWith("/recipes/sopa.webp", item.ImagenUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateItem_WhenTask_UpdatesItemAndAppearsInWeek()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-update-task");

        var createResponse = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Limpiar la heladera",
            hora = "10:30"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();

        var updateResponse = await _client.PatchAsJsonAsync($"/api/planificador/items/{created!.Id}", new
        {
            recetaId = (Guid?)null,
            tituloLibre = "Ordenar la alacena",
            hora = "11:15"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Ordenar la alacena", updated.TituloLibre);
        Assert.Equal("11:15", updated.Hora);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal("Ordenar la alacena", item.TituloLibre);
        Assert.Equal("11:15", item.Hora);
    }

    [Fact]
    public async Task UpdateItem_WhenRecipe_UpdatesRecipeAndAppearsInWeek()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-update-meal");
        var recetaId = await SeedRecipeAsync("Tarta de verduras", "/images/tarta.png");
        var nuevaRecetaId = await SeedRecipeAsync("Guiso de lentejas", "/images/guiso.png");

        var createResponse = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "almuerzo",
            recetaId,
            tituloLibre = (string?)null,
            hora = "13:00"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();

        var updateResponse = await _client.PatchAsJsonAsync($"/api/planificador/items/{created!.Id}", new
        {
            recetaId = nuevaRecetaId,
            tituloLibre = (string?)null,
            hora = "14:00"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.Equal(nuevaRecetaId, updated!.RecetaId);
        Assert.Equal("Guiso de lentejas", updated.RecetaNombre);
        Assert.Equal("/images/guiso.png", updated.ImagenUrl);
        Assert.Equal("14:00", updated.Hora);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal(nuevaRecetaId, item.RecetaId);
        Assert.Equal("Guiso de lentejas", item.RecetaNombre);
        Assert.Equal("/images/guiso.png", item.ImagenUrl);
    }

    [Fact]
    public async Task DeleteItem_RemovesItemFromWeek()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-delete");

        var createResponse = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Sacar basura",
            hora = "20:00"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();

        var deleteResponse = await _client.DeleteAsync($"/api/planificador/items/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        Assert.Empty(week!.Items);

        var tareasResponse = await _client.GetAsync("/api/tareas");
        var tareas = await tareasResponse.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.DoesNotContain(tareas!, tarea => tarea.Id == created.TareaId);
    }

    [Fact]
    public async Task CompletarTareaDesdeTareas_SeReflejaEnPlanificador()
    {
        var user = await RegisterAndAuthenticateAsync(_client, "plan-complete");

        var createResponse = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Limpiar patio",
            hora = "18:00",
            asignadoA = user.UsuarioId
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();
        Assert.NotNull(created);
        Assert.NotNull(created!.TareaId);

        var completeResponse = await _client.PostAsJsonAsync($"/api/tareas/{created.TareaId}/completar", new { });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var weekResponse = await _client.GetAsync("/api/planificador?fechaInicio=2026-06-15");
        var week = await weekResponse.Content.ReadFromJsonAsync<PlanificadorSemanaBody>();
        var item = Assert.Single(week!.Items);
        Assert.Equal("completada", item.TareaEstado);
    }

    [Fact]
    public async Task UpdateAndDeleteItem_WhenOtherHousehold_ReturnNotFound()
    {
        await RegisterAndAuthenticateAsync(_client, "plan-owner");

        var createResponse = await _client.PostAsJsonAsync("/api/planificador/items", new
        {
            fecha = "2026-06-19",
            tipoComida = "tarea",
            recetaId = (Guid?)null,
            tituloLibre = "Regar plantas",
            hora = "09:00"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PlanificadorItemBody>();

        await RegisterAndAuthenticateAsync(_client, "plan-other");

        var updateResponse = await _client.PatchAsJsonAsync($"/api/planificador/items/{created!.Id}", new
        {
            recetaId = (Guid?)null,
            tituloLibre = "No permitido",
            hora = "10:00"
        });
        var deleteResponse = await _client.DeleteAsync($"/api/planificador/items/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    private async Task<Guid> SeedRecipeAsync(string nombre, string? imagenUrl = null)
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
            Porciones = 4,
            ImagenUrl = imagenUrl
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
    private sealed record AsignacionBody(Guid UsuarioId, string Nombre, string? FotoStorageKey);
    private sealed record TareaBody(Guid Id, string Titulo, string Estado);
    private sealed record PlanificadorSemanaBody(Guid Id, string FechaInicio, List<PlanificadorItemBody> Items);
    private sealed record PlanificadorItemBody(
        Guid Id,
        string Fecha,
        string TipoComida,
        Guid? TareaId,
        Guid? RecetaId,
        string? RecetaNombre,
        string? ImagenUrl,
        string? TituloLibre,
        string? Hora,
        string? TareaEstado,
        AsignacionBody? AsignadoA,
        int Orden,
        Guid CreadoPor);
}
