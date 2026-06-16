using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingEquipmentTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public OnboardingEquipmentTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SaveEquipment_WhenTwoHouseholdsSubmitEquipment_StoresDataIsolatedPerHousehold()
    {
        using var registerAContent = RegisterMultipartRequest.Create("User A", "equip-a@test.com", "Password123!", "F");
        var registerA = await _client.PostAsync("/api/auth/register", registerAContent);
        var userA = await registerA.Content.ReadFromJsonAsync<RegisterBody>();

        using var registerBContent = RegisterMultipartRequest.Create("User B", "equip-b@test.com", "Password123!", "M");
        var registerB = await _client.PostAsync("/api/auth/register", registerBContent);
        var userB = await registerB.Content.ReadFromJsonAsync<RegisterBody>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userA!.AccessToken);
        var step3A = await _client.PatchAsJsonAsync("/api/onboarding/step-3", new
        {
            skip = false,
            equipments = new[] { new { nombre = "Heladera", tipo = "Fridge", estado = "new" } }
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userB!.AccessToken);
        var step3B = await _client.PatchAsJsonAsync("/api/onboarding/step-3", new
        {
            skip = false,
            equipments = new[] { new { nombre = "Licuadora", tipo = "Blender", estado = "used" } }
        });

        Assert.Equal(HttpStatusCode.NoContent, step3A.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, step3B.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var householdAEquipment = await db.Electrodomesticos.Where(x => x.HogarId == userA.HogarId).ToListAsync();
        var householdBEquipment = await db.Electrodomesticos.Where(x => x.HogarId == userB.HogarId).ToListAsync();

        Assert.Single(householdAEquipment);
        Assert.Single(householdBEquipment);
        Assert.Equal("Heladera", householdAEquipment[0].Nombre);
        Assert.Equal("Licuadora", householdBEquipment[0].Nombre);
    }

    [Fact]
    public async Task SaveEquipment_WhenSubmittedAgain_ReplacesPreviousHouseholdChoices()
    {
        using var registerContent = RegisterMultipartRequest.Create("Equip Retry", "equip-retry@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var firstResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-3", new
        {
            skip = false,
            equipments = new[]
            {
                new { nombre = "Heladera", tipo = "Fridge", estado = "new" }
            }
        });

        var secondResponse = await _client.PatchAsJsonAsync("/api/onboarding/step-3", new
        {
            skip = false,
            equipments = new[]
            {
                new { nombre = "Horno", tipo = "Oven", estado = "used" },
                new { nombre = "Tostadora", tipo = "Toaster", estado = "new" }
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var equipments = await db.Electrodomesticos
            .Where(x => x.HogarId == body.HogarId)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        Assert.Equal(2, equipments.Count);
        Assert.DoesNotContain(equipments, item => item.Nombre == "Heladera");
        Assert.Equal(["Horno", "Tostadora"], equipments.Select(x => x.Nombre).ToArray());

        var state = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == body.UsuarioId && x.HogarId == body.HogarId);
        Assert.False(state.Step3Skipped);
        Assert.NotNull(state.Step3CompletedAt);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
}
