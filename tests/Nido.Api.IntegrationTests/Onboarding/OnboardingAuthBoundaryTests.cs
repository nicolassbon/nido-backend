using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Onboarding;

public sealed class OnboardingAuthBoundaryTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public OnboardingAuthBoundaryTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SaveWellness_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsJsonAsync("/onboarding/step-4", new { skip = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveHousehold_WhenClientSendsForgedUserId_ReturnsForbidden()
    {
        using var registerContent = RegisterMultipartRequest.Create("Ana", "ana@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new
        {
            skip = false,
            usuarioId = Guid.NewGuid(),
            hogarId = body.HogarId,
            members = new[] { new { nombre = "Hija", rol = "child" } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var representedCount = await db.MiembrosHogars.CountAsync(x => x.HogarId == body.HogarId && x.NombreRepresentado != null);
        Assert.Equal(0, representedCount);
    }

    [Fact]
    public async Task SaveHousehold_WhenTokenIsMalformed_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveHousehold_WhenUserIsNotHouseholdMember_ReturnsForbidden()
    {
        using var registerContent = RegisterMultipartRequest.Create("Leo", "leo@test.com", "Password123!", "M");
        var register = await _client.PostAsync("/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var memberships = await db.MiembrosHogars.Where(x => x.UsuarioId == body.UsuarioId && x.HogarId == body.HogarId).ToListAsync();
            db.MiembrosHogars.RemoveRange(memberships);
            await db.SaveChangesAsync();
        }

        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaveHousehold_WhenInvitedMemberSubmitsStep2_SkipsHouseholdLevelOnboarding()
    {
        using var ownerRegisterContent = RegisterMultipartRequest.Create("Owner", "owner@test.com", "Password123!", "F");
        var ownerRegister = await _client.PostAsync("/auth/register", ownerRegisterContent);
        var owner = await ownerRegister.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(owner);

        using var invitedRegisterContent = RegisterMultipartRequest.Create("Guest", "guest@test.com", "Password123!", "M");
        var invitedRegister = await _client.PostAsync("/auth/register", invitedRegisterContent);
        var invited = await invitedRegister.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(invited);

        string invitedToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var ownedMembership = await db.MiembrosHogars.SingleAsync(x => x.UsuarioId == invited.UsuarioId && x.HogarId == invited.HogarId);
            ownedMembership.Rol = "member";
            ownedMembership.HogarId = owner.HogarId;

            var invitedState = await db.OnboardingStates.SingleAsync(x => x.UsuarioId == invited.UsuarioId && x.HogarId == invited.HogarId);
            invitedState.HogarId = owner.HogarId;
            invitedState.Step2CompletedAt = null;
            invitedState.Step2Skipped = false;

            await db.SaveChangesAsync();
            invitedToken = tokenService.CreateToken(invited.UsuarioId, owner.HogarId, "guest@test.com", "Test");
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invitedToken);
        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new
        {
            skip = false,
            members = new[] { new { nombre = "No debe guardarse", rol = "child" } }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var representedCount = await verifyDb.MiembrosHogars.CountAsync(x => x.HogarId == owner.HogarId && x.NombreRepresentado != null);
        Assert.Equal(0, representedCount);

        var state = await verifyDb.OnboardingStates.SingleAsync(x => x.UsuarioId == invited.UsuarioId && x.HogarId == owner.HogarId);
        Assert.True(state.Step2Skipped);
        Assert.NotNull(state.Step2CompletedAt);
    }

    [Fact]
    public async Task SaveHousehold_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsJsonAsync("/onboarding/step-2", new { skip = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveEquipment_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsJsonAsync("/onboarding/step-3", new { skip = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveWellness_WhenClientSendsForgedUserId_ReturnsForbidden()
    {
        using var registerContent = RegisterMultipartRequest.Create("Sofi", "sofi@test.com", "Password123!", "F");
        var register = await _client.PostAsync("/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await _client.PatchAsJsonAsync("/onboarding/step-4", new
        {
            skip = false,
            usuarioId = Guid.NewGuid(),
            hogarId = body.HogarId,
            restricciones = new[] { new { tipo = "allergy", descripcion = "nuts" } },
            goals = new[] { new { titulo = "Eat better", descripcion = "More veggies" } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
}
