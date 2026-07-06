using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Gamificacion;

public sealed class GamificacionProgressEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public GamificacionProgressEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProgreso_Authenticated_ReturnsOwnData()
    {
        var user = await AuthenticateAsync(_client, "gami-progress-own");

        var response = await _client.GetAsync("/api/gamificacion/progreso");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GamificacionProgresoBody>();
        Assert.NotNull(body);
        Assert.Equal(user.UsuarioId, body!.UsuarioId);
        Assert.Equal(0, body.CurrentXp);
        Assert.Equal(0, body.CurrentLevel);
        Assert.True(body.HasNextLevel);
        Assert.Equal(1, body.NextLevel);
        Assert.Equal(20, body.NextThresholdXp);
        Assert.Equal(20, body.XpToNextLevel);
    }

    [Fact]
    public async Task GetProgreso_Anonymous_Returns401()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/gamificacion/progreso");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProgreso_WithQueryUserId_StillReturnsCallerData()
    {
        var caller = await AuthenticateAsync(_client, "gami-progress-caller");
        using var otherClient = _factory.CreateClient();
        var other = await AuthenticateAsync(otherClient, "gami-progress-other");
        await CreateTaskAsync(otherClient, "Other task");

        var response = await _client.GetAsync($"/api/gamificacion/progreso?userId={other.UsuarioId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GamificacionProgresoBody>();
        Assert.NotNull(body);
        Assert.Equal(caller.UsuarioId, body!.UsuarioId);
        Assert.Equal(0, body.CurrentXp);
    }

    [Fact]
    public async Task GetProgreso_RetroactiveConfigChange_RecomputesCurrentXpOnNextRead()
    {
        var user = await AuthenticateAsync(_client, "gami-progress-config-xp");
        var task = await CreateTaskAsync(_client, "Config XP task");
        await _client.PostAsJsonAsync($"/api/tareas/{task.Id}/completar", new { });

        using var changedFactory = _factory.WithConfiguration(DefaultGamificationConfiguration(xpPerCompletedTask: 25));
        using var changedClient = changedFactory.CreateClient();
        changedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var body = await changedClient.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");

        Assert.NotNull(body);
        Assert.Equal(user.UsuarioId, body!.UsuarioId);
        Assert.Equal(25, body.CurrentXp);
    }

    [Fact]
    public async Task GetProgreso_AfterCompletion_MaterializesAllMissingEligibleLevels_NotOnlyHighest()
    {
        using var customFactory = _factory.WithConfiguration(new Dictionary<string, string?>
        {
            ["Gamification:XpPerCompletedTask"] = "60",
            ["Gamification:Levels:0:Level"] = "1",
            ["Gamification:Levels:0:RequiredXp"] = "20",
            ["Gamification:Levels:0:Name"] = "Huevito",
            ["Gamification:Levels:0:AvatarUrl"] = "https://cdn.test/huevito.png",
            ["Gamification:Levels:1:Level"] = "2",
            ["Gamification:Levels:1:RequiredXp"] = "60",
            ["Gamification:Levels:1:Name"] = "Pollito",
            ["Gamification:Levels:1:AvatarUrl"] = "https://cdn.test/pollito.png"
        });
        using var client = customFactory.CreateClient();
        var user = await AuthenticateAsync(client, "gami-progress-all-missing");
        var task = await CreateTaskAsync(client, "All missing levels task");
        await client.PostAsJsonAsync($"/api/tareas/{task.Id}/completar", new { });

        var body = await client.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");

        Assert.NotNull(body);
        Assert.Equal(60, body!.CurrentXp);
        Assert.Equal(2, body.CurrentLevel);
        using var scope = customFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var levels = await db.GamificacionNivelesDesbloqueados
            .Where(x => x.UsuarioId == user.UsuarioId)
            .Select(x => x.Nivel)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal([1, 2], levels);
    }

    [Fact]
    public async Task GetProgreso_XpDropBelowCurrentThreshold_CurrentLevelUnchanged()
    {
        var user = await AuthenticateAsync(_client, "gami-progress-xp-drop");
        var task = await CreateTaskAsync(_client, "XP drop task");
        await _client.PostAsJsonAsync($"/api/tareas/{task.Id}/completar", new { });
        await _client.PatchAsJsonAsync($"/api/tareas/{task.Id}", new { estado = "pendiente" });

        var body = await _client.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");

        Assert.NotNull(body);
        Assert.Equal(user.UsuarioId, body!.UsuarioId);
        Assert.Equal(0, body.CurrentXp);
        Assert.Equal(1, body.CurrentLevel);
    }

    [Fact]
    public async Task GetProgreso_ConfigRemovedCurrentLevelMetadata_ReturnsNullNameAndUrl_LevelNumberRemains()
    {
        var user = await AuthenticateAsync(_client, "gami-progress-removed-meta");
        var task = await CreateTaskAsync(_client, "Removed metadata task");
        await _client.PostAsJsonAsync($"/api/tareas/{task.Id}/completar", new { });

        using var changedFactory = _factory.WithConfiguration(new Dictionary<string, string?>
        {
            ["Gamification:XpPerCompletedTask"] = "20",
            ["Gamification:Levels:0:Level"] = "2",
            ["Gamification:Levels:0:RequiredXp"] = "60",
            ["Gamification:Levels:0:Name"] = "Pollito",
            ["Gamification:Levels:0:AvatarUrl"] = "https://cdn.test/pollito.png"
        });
        using var changedClient = changedFactory.CreateClient();
        changedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var body = await changedClient.GetFromJsonAsync<GamificacionProgresoBody>("/api/gamificacion/progreso");

        Assert.NotNull(body);
        Assert.Equal(1, body!.CurrentLevel);
        Assert.Null(body.CurrentLevelNombre);
        Assert.Null(body.CurrentLevelAvatarUrl);
    }

    [Fact]
    public async Task GetProgreso_RepeatedCallWithoutStateChange_DoesNotCreateAdditionalUnlockRows()
    {
        var user = await AuthenticateAsync(_client, "gami-progress-idempotent");
        var task = await CreateTaskAsync(_client, "Repeated progress task");
        await _client.PostAsJsonAsync($"/api/tareas/{task.Id}/completar", new { });

        await _client.GetAsync("/api/gamificacion/progreso");
        await _client.GetAsync("/api/gamificacion/progreso");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.Equal(1, await db.GamificacionNivelesDesbloqueados.CountAsync(x => x.UsuarioId == user.UsuarioId && x.Nivel == 1));
    }

    private static IReadOnlyDictionary<string, string?> DefaultGamificationConfiguration(int xpPerCompletedTask)
        => new Dictionary<string, string?>
        {
            ["Gamification:XpPerCompletedTask"] = xpPerCompletedTask.ToString(),
            ["Gamification:Levels:0:Level"] = "1",
            ["Gamification:Levels:0:RequiredXp"] = "20",
            ["Gamification:Levels:0:Name"] = "Huevito",
            ["Gamification:Levels:0:AvatarUrl"] = "https://cdn.test/huevito.png",
            ["Gamification:Levels:1:Level"] = "2",
            ["Gamification:Levels:1:RequiredXp"] = "60",
            ["Gamification:Levels:1:Name"] = "Pollito",
            ["Gamification:Levels:1:AvatarUrl"] = "https://cdn.test/pollito.png"
        };

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
    private sealed record TareaBody(Guid Id);
    private sealed record GamificacionProgresoBody(
        Guid UsuarioId,
        int CurrentXp,
        int CurrentLevel,
        string? CurrentLevelNombre,
        string? CurrentLevelAvatarUrl,
        int? NextLevel,
        string? NextLevelNombre,
        string? NextLevelAvatarUrl,
        int? NextThresholdXp,
        int? XpToNextLevel,
        bool HasNextLevel);
}
