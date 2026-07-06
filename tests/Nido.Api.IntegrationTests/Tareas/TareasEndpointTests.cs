using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

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

    [Fact]
    public async Task CreateTarea_WhenAsignadoAIsUserFromDifferentHousehold_Returns404AndCreatesNoSideEffects()
    {
        var owner = await AuthenticateAsync(_client, "tareas-assign-cross-owner");
        using var intruderClient = _factory.CreateClient();
        var intruder = await AuthenticateAsync(intruderClient, "tareas-assign-cross-intruder");

        var response = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea con assignee externo",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = intruder.UsuarioId,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("NOT_HOUSEHOLD_MEMBER", problem!.Title);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var ownerTareas = await db.Tareas
            .Where(t => t.HogarId == owner.HogarId)
            .ToListAsync();
        Assert.Empty(ownerTareas);

        var intruderAssignments = await db.AsignacionesTareas
            .Where(a => a.UsuarioId == intruder.UsuarioId)
            .ToListAsync();
        Assert.Empty(intruderAssignments);

        var intruderNotifications = await db.Notificaciones
            .Where(n => n.UsuarioId == intruder.UsuarioId)
            .ToListAsync();
        Assert.Empty(intruderNotifications);

        var intruderOutbox = await db.TelegramOutboxMessages
            .Where(o => o.HogarId == owner.HogarId || o.HogarId == intruder.HogarId)
            .ToListAsync();
        Assert.Empty(intruderOutbox);
    }

    [Fact]
    public async Task CreateTarea_WhenAsignadoAIsNonExistentUser_Returns404AndCreatesNoSideEffects()
    {
        var owner = await AuthenticateAsync(_client, "tareas-assign-ghost-owner");

        var response = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea con assignee inexistente",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = Guid.NewGuid(),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("NOT_HOUSEHOLD_MEMBER", problem!.Title);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var ownerTareas = await db.Tareas
            .Where(t => t.HogarId == owner.HogarId)
            .ToListAsync();
        Assert.Empty(ownerTareas);

        var ownerAssignments = await db.AsignacionesTareas
            .Where(a => a.UsuarioId == owner.UsuarioId)
            .ToListAsync();
        Assert.Empty(ownerAssignments);

        var ownerNotifications = await db.Notificaciones
            .Where(n => n.UsuarioId == owner.UsuarioId)
            .ToListAsync();
        Assert.Empty(ownerNotifications);
    }

    [Fact]
    public async Task AsignarTarea_WhenAsignadoAIsUserFromDifferentHousehold_Returns404AndCreatesNoSideEffects()
    {
        var owner = await AuthenticateAsync(_client, "tareas-reassign-cross-owner");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Tarea legitima sin asignar",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        using var intruderClient = _factory.CreateClient();
        var intruder = await AuthenticateAsync(intruderClient, "tareas-reassign-cross-intruder");

        var response = await _client.PostAsJsonAsync(
            $"/api/tareas/{tarea!.Id}/asignar",
            new { UsuarioId = intruder.UsuarioId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal("NOT_HOUSEHOLD_MEMBER", problem!.Title);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var intruderAssignments = await db.AsignacionesTareas
            .Where(a => a.UsuarioId == intruder.UsuarioId)
            .ToListAsync();
        Assert.Empty(intruderAssignments);

        var intruderNotifications = await db.Notificaciones
            .Where(n => n.UsuarioId == intruder.UsuarioId)
            .ToListAsync();
        Assert.Empty(intruderNotifications);

        var ownerTarea = await db.Tareas
            .SingleAsync(t => t.Id == tarea.Id);
        Assert.Empty(ownerTarea.AsignacionesTareas);

        var ownerOutbox = await db.TelegramOutboxMessages
            .Where(o => o.HogarId == owner.HogarId)
            .ToListAsync();
        Assert.Empty(ownerOutbox);
    }


    // ── Gamification (T10) ────────────────────────────────────────────────

    [Fact]
    public async Task CompletarTarea_ResponseIncludesXpOtorgado_EqualsConfiguredXp()
    {
        var user = await AuthenticateAsync(_client, "gami-complete-xp");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Task for XP test",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        var completeResp = await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });
        var completed = await completeResp.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(completed);
        Assert.Equal(20, completed!.XpOtorgado);
    }

    [Fact]
    public async Task CompletarTarea_TwiceForSameUser_IsIdempotent_DoesNotDoubleCount()
    {
        var user = await AuthenticateAsync(_client, "gami-idempotent");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Idempotent task",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });
        var second = await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });
        var secondBody = await second.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(secondBody);
        Assert.Equal(20, secondBody!.XpOtorgado);

        var progress = await _client.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");
        Assert.NotNull(progress);
        Assert.Equal(20, progress!.CurrentXp);
        Assert.Equal(1, await CountUnlocksAsync(user.UsuarioId));
    }

    [Fact]
    public async Task GetTareas_CompletedTasksExposeXpOtorgado_NonCompletedExposeNull()
    {
        var user = await AuthenticateAsync(_client, "gami-list-xp");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Pending task",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var pendingTarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(pendingTarea);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var storedTask = await db.Tareas.SingleAsync(t => t.Id == pendingTarea!.Id);
            storedTask.Estado = "completada";
            storedTask.CompletadoPor = user.UsuarioId;
            storedTask.FechaCompletado = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        Assert.Equal(0, await CountUnlocksAsync(user.UsuarioId));
        // Create another non-completed task
        await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Another pending",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });

        var listResp = await _client.GetAsync("/api/tareas");
        var tareas = await listResp.Content.ReadFromJsonAsync<List<TareaBody>>();
        Assert.NotNull(tareas);

        var completed = tareas!.First(t => t.Estado == "completada");
        Assert.Equal(20, completed.XpOtorgado);
        var pending = tareas!.First(t => t.Estado != "completada");
        Assert.Null(pending.XpOtorgado);
        Assert.Equal(0, await CountUnlocksAsync(user.UsuarioId));
    }

    [Fact]
    public async Task PatchTarea_WithEstadoCompletada_Returns400()
    {
        var user = await AuthenticateAsync(_client, "gami-patch-reject");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Patch reject task",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        var patchResp = await _client.PatchAsJsonAsync($"/api/tareas/{tarea!.Id}",
            new { estado = "completada" });
        Assert.Equal(HttpStatusCode.BadRequest, patchResp.StatusCode);

        var afterPatch = (await _client.GetFromJsonAsync<List<TareaBody>>("/api/tareas"))!
            .Single(t => t.Id == tarea.Id);
        Assert.Equal("pendiente", afterPatch.Estado);
        Assert.Null(afterPatch.CompletadoPor);
        Assert.Null(afterPatch.XpOtorgado);
        Assert.Equal(0, await CountUnlocksAsync(user.UsuarioId));
    }

    [Fact]
    public async Task PatchTarea_ReopenClearsCompletionFields_AndXpOtorgadoIsNull()
    {
        var user = await AuthenticateAsync(_client, "gami-reopen");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Reopen test",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });

        var patchResp = await _client.PatchAsJsonAsync($"/api/tareas/{tarea.Id}",
            new { estado = "pendiente" });
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);
        var reopened = await patchResp.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(reopened);
        Assert.Null(reopened!.XpOtorgado);

        var progress = await _client.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");
        Assert.NotNull(progress);
        Assert.Equal(0, progress!.CurrentXp);
        Assert.Equal(1, progress.CurrentLevel);
    }

    [Fact]
    public async Task CompletarTarea_ThresholdCrossing_CreatesExactlyOneUnlockRow_InDatabase()
    {
        var user = await AuthenticateAsync(_client, "gami-threshold");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "Threshold test",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Nido.Infrastructure.Persistence.NidoDbContext>();
        var unlocks = await db.GamificacionNivelesDesbloqueados
            .Where(u => u.UsuarioId == user.UsuarioId).ToListAsync();
        Assert.Single(unlocks);
        Assert.Equal(1, unlocks[0].Nivel);
    }

    [Fact]
    public async Task Completar_Reopen_Recomplete_DoesNotDuplicateUnlockRow()
    {
        var user = await AuthenticateAsync(_client, "gami-nodup");
        var created = await _client.PostAsJsonAsync("/api/tareas", new
        {
            titulo = "No dup test",
            descripcion = (string?)null,
            fechaLimite = (string?)null,
            asignadoA = (Guid?)null,
        });
        var tarea = await created.Content.ReadFromJsonAsync<TareaBody>();
        Assert.NotNull(tarea);

        await _client.PostAsJsonAsync($"/api/tareas/{tarea!.Id}/completar", new { });
        await _client.PatchAsJsonAsync($"/api/tareas/{tarea.Id}", new { estado = "pendiente" });
        await _client.PostAsJsonAsync($"/api/tareas/{tarea.Id}/completar", new { });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Nido.Infrastructure.Persistence.NidoDbContext>();
        var unlockCount = await db.GamificacionNivelesDesbloqueados
            .Where(u => u.UsuarioId == user.UsuarioId).CountAsync();
        Assert.Equal(1, unlockCount);
    }


    private async Task<int> CountUnlocksAsync(Guid usuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        return await db.GamificacionNivelesDesbloqueados.CountAsync(u => u.UsuarioId == usuarioId);
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
    private sealed record GamificacionProgresoBody(Guid UsuarioId, int CurrentXp, int CurrentLevel);
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
        DateTime CreatedAt,
            int? XpOtorgado);
    private sealed record MiembroDistBody(Guid UsuarioId, string Nombre, int Completadas);
    private sealed record DistribucionDiaBody(string Dia, DateTime Fecha, List<MiembroDistBody> Miembros);
    private sealed record DistribucionBody(List<DistribucionDiaBody> Dias);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
