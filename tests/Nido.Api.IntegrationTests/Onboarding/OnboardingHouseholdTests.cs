using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task SaveHousehold_WhenRepresentedMemberIsProvided_PersistsMembershipData()
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Marta", email = "marta@test.com", password = "Password123!", sexo = "F" });
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new
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
    public async Task SaveHousehold_WhenOwnerLivesAlone_KeepsSingleOwnerMembershipValid()
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Solo", email = "solo@test.com", password = "Password123!", sexo = "M" });
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var step2 = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = false, members = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NoContent, step2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var ownerMembership = await db.MiembrosHogars.SingleAsync(x => x.HogarId == body.HogarId && x.UsuarioId == body.UsuarioId);
        Assert.Equal("owner", ownerMembership.Rol);

        var representedCount = await db.MiembrosHogars.CountAsync(x => x.HogarId == body.HogarId && x.NombreRepresentado != null);
        Assert.Equal(0, representedCount);
    }

    [Fact]
    public async Task SaveHouseholdAndEquipment_WhenSkippingHouseholdAndSavingEquipment_PersistIndependently()
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { nombre = "Bob", email = "bob@test.com", password = "Password123!", sexo = "M" });
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var step2 = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });
        var step3 = await _client.PatchAsJsonAsync("/onboarding/step-3", new { skip = false, equipments = new[] { new { nombre = "Horno", tipo = "Oven", estado = "new" } } });

        Assert.Equal(HttpStatusCode.NoContent, step2.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, step3.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var state = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == body.UsuarioId && x.HogarId == body.HogarId);
        Assert.True(state.Step2Skipped);
        Assert.False(state.Step3Skipped);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
}
