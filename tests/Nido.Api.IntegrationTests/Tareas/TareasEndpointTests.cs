using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests.Auth;

namespace Nido.Api.IntegrationTests.Tareas;

public sealed class TareasEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public TareasEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTareas_WhenAnonymous_Returns401()
    {
        using var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync("/api/tareas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTarea_WithValidData_Returns201AndAppearsInGetList()
    {
        await AuthenticateAsync(_client, "tareas-create");

        var response = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Limpiar la cocina",
            descripcion = "Mesada y pisos",
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tarea = await response.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);
        Assert.Equal("Limpiar la cocina", tarea!.Titulo);
        Assert.Equal("Mesada y pisos", tarea.Descripcion);
        Assert.Equal("pendiente", tarea.Estado);
        Assert.False(tarea.Vencida);

        var listResponse = await _client.GetAsync("/api/tareas");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var tareas = await listResponse.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.Contains(tareas!, t => t.Id == tarea.Id);
    }

    [Fact]
    public async Task DeleteTarea_WhenExists_Returns204AndDisappearsFromList()
    {
        await AuthenticateAsync(_client, "tareas-delete");

        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Sacar la basura",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        var deleteResponse = await _client.DeleteAsync($"/api/tareas/{tarea!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/tareas");
        var tareas = await listResponse.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.DoesNotContain(tareas!, t => t.Id == tarea.Id);
    }

    [Fact]
    public async Task DeleteTarea_WhenNotFound_Returns404()
    {
        await AuthenticateAsync(_client, "tareas-delete-404");

        var response = await _client.DeleteAsync($"/api/tareas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompletarTarea_WhenExists_SetsEstadoCompletadaAndRecordsWhoCompleted()
    {
        var user = await AuthenticateAsync(_client, "tareas-completar");

        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Regar las plantas",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);
        Assert.Equal("pendiente", tarea!.Estado);

        var completarResponse = await _client.PostAsJsonAsync($"/api/tareas/{tarea.Id}/completar", new { });

        Assert.Equal(HttpStatusCode.OK, completarResponse.StatusCode);
        var completada = await completarResponse.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(completada);
        Assert.Equal("completada", completada!.Estado);
        Assert.NotNull(completada.FechaCompletado);
        Assert.Equal(user.UsuarioId, completada.CompletadoPor);
        Assert.NotNull(completada.CompletadoPorNombre);
    }

    [Fact]
    public async Task CompletarTarea_WhenNotFound_Returns404()
    {
        await AuthenticateAsync(_client, "tareas-completar-404");

        var response = await _client.PostAsJsonAsync($"/api/tareas/{Guid.NewGuid()}/completar", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMisTareas_ReturnsOnlyTareasAssignedToCurrentUser()
    {
        var user = await AuthenticateAsync(_client, "tareas-mis");

        await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Mi tarea asignada",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = user.UsuarioId,
        });

        await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea sin asignar",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });

        var response = await _client.GetAsync("/api/tareas/mis-tareas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tareas = await response.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.NotNull(tareas);
        Assert.Single(tareas!);
        Assert.Equal("Mi tarea asignada", tareas![0].Titulo);
    }

    [Fact]
    public async Task AsignarTarea_WhenHouseholdMember_UpdatesAsignadoA()
    {
        var user = await AuthenticateAsync(_client, "tareas-asignar");

        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Aspirar el living",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);
        Assert.Null(tarea!.AsignadoA);

        var asignarResponse = await _client.PostAsJsonAsync(
            $"/api/tareas/{tarea.Id}/asignar",
            new { UsuarioId = user.UsuarioId });

        Assert.Equal(HttpStatusCode.OK, asignarResponse.StatusCode);
        var asignada = await asignarResponse.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(asignada);
        Assert.NotNull(asignada!.AsignadoA);
        Assert.Equal(user.UsuarioId, asignada.AsignadoA!.UsuarioId);
    }

    [Fact]
    public async Task AsignarTarea_WithNullUserId_RemovesAssignment()
    {
        var user = await AuthenticateAsync(_client, "tareas-desasignar");

        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Barrer",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = user.UsuarioId,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);
        Assert.NotNull(tarea!.AsignadoA);

        var desasignarResponse = await _client.PostAsJsonAsync(
            $"/api/tareas/{tarea.Id}/asignar",
            new { UsuarioId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, desasignarResponse.StatusCode);
        var desasignada = await desasignarResponse.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(desasignada);
        Assert.Null(desasignada!.AsignadoA);
    }

    [Fact]
    public async Task GetDistribucionSemanal_Returns7Days()
    {
        await AuthenticateAsync(_client, "tareas-dist");

        var response = await _client.GetAsync("/api/tareas/distribucion-semanal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var distribucion = await response.Content.ReadFromJsonAsync<DistribucionBody>();
        Assert.NotNull(distribucion);
        Assert.Equal(7, distribucion!.Dias.Count);
    }

    [Fact]
    public async Task GetTareas_DoesNotReturnTareasFromOtherHogares()
    {
        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, "tareas-aislamiento-otro");
        await otherClient.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea exclusiva del otro hogar",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });

        await AuthenticateAsync(_client, "tareas-aislamiento-yo");
        var response = await _client.GetAsync("/api/tareas");
        var tareas = await response.Content.ReadFromJsonAsync<List<TareaBody>>();

        Assert.NotNull(tareas);
        Assert.DoesNotContain(tareas!, t => t.Titulo == "Tarea exclusiva del otro hogar");
    }

    [Fact]
    public async Task GetMisTareas_ExcludesCompletedTareas()
    {
        var user = await AuthenticateAsync(_client, "tareas-mis-completadas");

        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea a completar",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = user.UsuarioId,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });

        var misTareasResponse = await _client.GetAsync("/api/tareas/mis-tareas");
        var misTareas = await misTareasResponse.Content.ReadFromJsonAsync<List<TareaBody>>();

        Assert.NotNull(misTareas);
        Assert.DoesNotContain(misTareas!, t => t.Id == tarea.Id);
    }

    private async Task<AuthenticatedUser> AuthenticateAsync(HttpClient client, string prefix, string name = "Test User")
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var req = RegisterMultipartRequest.Create(name, email, "Password123!", "U");
        var res = await client.PostAsync("/api/auth/register", req);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return new AuthenticatedUser(body.UsuarioId, body.HogarId, body.AccessToken, email, name);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record AuthenticatedUser(Guid UsuarioId, Guid HogarId, string AccessToken, string Email, string Nombre);
    private sealed record AsignacionBody(Guid UsuarioId, string Nombre, string? FotoStorageKey);
    private sealed record TareaBody(
        Guid Id,
        string Titulo,
        string? Descripcion,
        string Estado,
        DateTime? FechaLimite,
        DateTime? FechaCompletado,
        Guid CreadoPor,
        string CreadoPorNombre,
        Guid? CompletadoPor,
        string? CompletadoPorNombre,
        AsignacionBody? AsignadoA,
        bool Vencida,
        DateTime CreatedAt);
    private sealed record MiembroDistBody(Guid UsuarioId, string Nombre, int Completadas);
    private sealed record DistribucionDiaBody(string Dia, DateTime Fecha, List<MiembroDistBody> Miembros);
    private sealed record DistribucionBody(List<DistribucionDiaBody> Dias);
}
