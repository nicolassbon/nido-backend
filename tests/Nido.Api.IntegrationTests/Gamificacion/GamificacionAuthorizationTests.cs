using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Gamificacion;

public sealed class GamificacionAuthorizationTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public GamificacionAuthorizationTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_CannotCallProgreso_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gamificacion/progreso");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotListTareas_401_NoXpOtorgadoLeak()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tareas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadAsStringAsync()).Length);
    }

    [Fact]
    public async Task Anonymous_CannotCompletarTarea_401()
    {
        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsync(ownerClient, "gami-auth-owner");
        var tarea = await CreateTaskAsync(ownerClient, "Anonymous complete target");
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync($"/api/tareas/{tarea.Id}/completar", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MemberOfHogarA_CannotReadHogarBTarea_403Or404_NoXpOtorgadoLeak()
    {
        using var hogarBClient = _factory.CreateClient();
        var hogarBUser = await AuthenticateAsync(hogarBClient, "gami-auth-read-b");
        var tareaB = await CreateTaskAsync(hogarBClient, "Hogar B secret task");
        await hogarBClient.PostAsJsonAsync($"/api/tareas/{tareaB.Id}/completar", new { });

        using var hogarAClient = _factory.CreateClient();
        await AuthenticateAsync(hogarAClient, "gami-auth-read-a");

        var response = await hogarAClient.GetAsync("/api/tareas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tareas = await response.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.NotNull(tareas);
        Assert.DoesNotContain(tareas!, t => t.Id == tareaB.Id);
        Assert.DoesNotContain(tareas, t => t.XpOtorgado.HasValue && t.CompletadoPor == hogarBUser.UsuarioId);
    }

    [Fact]
    public async Task MemberOfHogarA_CannotCompletarHogarBTarea_403Or404_NoUnlockSideEffect()
    {
        using var hogarBClient = _factory.CreateClient();
        var hogarBUser = await AuthenticateAsync(hogarBClient, "gami-auth-complete-b");
        var tareaB = await CreateTaskAsync(hogarBClient, "Hogar B complete target");

        using var hogarAClient = _factory.CreateClient();
        var hogarAUser = await AuthenticateAsync(hogarAClient, "gami-auth-complete-a");

        var response = await hogarAClient.PostAsJsonAsync($"/api/tareas/{tareaB.Id}/completar", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var tarea = await db.Tareas.SingleAsync(t => t.Id == tareaB.Id);
        Assert.Equal("pendiente", tarea.Estado);
        Assert.Null(tarea.CompletadoPor);
        Assert.Equal(0, await db.GamificacionNivelesDesbloqueados.CountAsync(u => u.UsuarioId == hogarAUser.UsuarioId));
        Assert.Equal(0, await db.GamificacionNivelesDesbloqueados.CountAsync(u => u.UsuarioId == hogarBUser.UsuarioId));
    }

    [Fact]
    public async Task MemberOfHogarA_CannotPatchHogarBTarea_403Or404_NoUnlockSideEffect()
    {
        using var hogarBClient = _factory.CreateClient();
        var hogarBUser = await AuthenticateAsync(hogarBClient, "gami-auth-patch-b");
        var tareaB = await CreateTaskAsync(hogarBClient, "Hogar B patch target");

        using var hogarAClient = _factory.CreateClient();
        var hogarAUser = await AuthenticateAsync(hogarAClient, "gami-auth-patch-a");

        var response = await hogarAClient.PatchAsJsonAsync($"/api/tareas/{tareaB.Id}", new { estado = "pendiente" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var tarea = await db.Tareas.SingleAsync(t => t.Id == tareaB.Id);
        Assert.Equal("pendiente", tarea.Estado);
        Assert.Null(tarea.CompletadoPor);
        Assert.Equal(0, await db.GamificacionNivelesDesbloqueados.CountAsync(u => u.UsuarioId == hogarAUser.UsuarioId));
        Assert.Equal(0, await db.GamificacionNivelesDesbloqueados.CountAsync(u => u.UsuarioId == hogarBUser.UsuarioId));
    }

    [Fact]
    public async Task Progreso_DoesNotAccept_ExternalUserId_InQuery_Returns200ForCaller()
    {
        using var callerClient = _factory.CreateClient();
        var caller = await AuthenticateAsync(callerClient, "gami-auth-progress-caller");
        using var otherClient = _factory.CreateClient();
        var other = await AuthenticateAsync(otherClient, "gami-auth-progress-other");
        var otherTask = await CreateTaskAsync(otherClient, "Other progress task");
        await otherClient.PostAsJsonAsync($"/api/tareas/{otherTask.Id}/completar", new { });

        var response = await callerClient.GetAsync($"/api/gamificacion/progreso?userId={other.UsuarioId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GamificacionProgresoBody>();
        Assert.NotNull(body);
        Assert.Equal(caller.UsuarioId, body!.UsuarioId);
        Assert.Equal(0, body.CurrentXp);
    }

    private static async Task<TareaBody> CreateTaskAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = title,
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tarea = await response.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);
        return tarea!;
    }

    private static async Task<AuthenticatedUser> AuthenticateAsync(HttpClient client, string prefix, string name = "Test User")
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
    private sealed record TareaBody(Guid Id, Guid? CompletadoPor, int? XpOtorgado);
    private sealed record GamificacionProgresoBody(Guid UsuarioId, int CurrentXp);
}
