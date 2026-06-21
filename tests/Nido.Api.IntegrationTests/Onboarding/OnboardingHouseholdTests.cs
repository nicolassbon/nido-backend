using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingHouseholdTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public OnboardingHouseholdTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GuardarHogar_CuandoSeAgregaUnMiembroRepresentado_PersisteLosDatasDeMiembro()
    {
        using var registerContent = RegisterMultipartRequest.Create("Marta", "marta@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PatchAsJsonAsync("/api/onboarding/step-2", new
        {
            skip = false,
            members = new[] { new { nombre = "Pepe", rol = "child" } }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var represented = await db.MiembrosHogars.SingleAsync(x => x.HogarId == body.HogarId && x.NombreRepresentado != null);
        Assert.Equal("Pepe", represented.NombreRepresentado);
        Assert.Equal("child", represented.Rol);
    }

    [Fact]
    public async Task GuardarHogar_CuandoElDuenioViveSolo_MantieneUnaSolaMembresiaValida()
    {
        using var registerContent = RegisterMultipartRequest.Create("Solo", "solo@test.com", "Password123!", "M");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var step2 = await _client.PatchAsJsonAsync("/api/onboarding/step-2", new { skip = false, members = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NoContent, step2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var ownerMembership = await db.MiembrosHogars.SingleAsync(x => x.HogarId == body.HogarId && x.UsuarioId == body.UsuarioId);
        Assert.Equal("owner", ownerMembership.Rol);

        var representedCount = await db.MiembrosHogars.CountAsync(x => x.HogarId == body.HogarId && x.NombreRepresentado != null);
        Assert.Equal(0, representedCount);
    }

    [Fact]
    public async Task GuardarHogarYEquipamiento_CuandoSeSaltaElHogarYSeGuardaEquipamiento_PersistenPorSeparado()
    {
        using var registerContent = RegisterMultipartRequest.Create("Bob", "bob@test.com", "Password123!", "M");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var step2 = await _client.PatchAsJsonAsync("/api/onboarding/step-2", new { skip = true });
        var step3 = await _client.PatchAsJsonAsync("/api/onboarding/step-3", new { skip = false, equipments = new[] { new { nombre = "Horno", tipo = "Oven", estado = "new" } } });

        Assert.Equal(HttpStatusCode.NoContent, step2.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, step3.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var state = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == body.UsuarioId && x.HogarId == body.HogarId);
        Assert.True(state.Step2Skipped);
        Assert.False(state.Step3Skipped);
    }

    [Fact]
    public async Task SaveHousehold_WhenSubmittedAgain_ReplacesRepresentedMembersAndKeepsStepCompleted()
    {
        using var registerContent = RegisterMultipartRequest.Create("Retry Household", "retry-house@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var firstResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-2", new
        {
            skip = false,
            members = new[]
            {
                new { nombre = "Pepe", rol = "child" },
                new { nombre = "Lola", rol = "adult" }
            }
        });

        var secondResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-2", new
        {
            skip = false,
            members = new[]
            {
                new { nombre = "Uma", rol = "child" }
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var representedMembers = await db.MiembrosHogars
            .Where(x => x.HogarId == body.HogarId && x.NombreRepresentado != null)
            .OrderBy(x => x.NombreRepresentado)
            .ToListAsync();

        Assert.Single(representedMembers);
        Assert.Equal("Uma", representedMembers[0].NombreRepresentado);
        Assert.Equal("child", representedMembers[0].Rol);

        var state = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == body.UsuarioId && x.HogarId == body.HogarId);
        Assert.False(state.Step2Skipped);
        Assert.NotNull(state.Step2CompletedAt);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
}
