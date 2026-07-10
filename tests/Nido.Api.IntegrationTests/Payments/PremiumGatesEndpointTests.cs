using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Payments;

public sealed class PremiumGatesEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public PremiumGatesEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToggleModoAhorro_WhenHouseholdIsFree_Returns403WithPlanUpgradeCode()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, "premium-gate-finanzas-free");

        var response = await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("PLAN_UPGRADE_REQUIRED", body!.Title);
    }

    [Fact]
    public async Task ToggleModoAhorro_WhenHouseholdIsPremium_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var user = await AuthenticateAsync(client, "premium-gate-finanzas-premium");
        await MakePremiumAsync(user.HogarId);

        var response = await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ModoAhorroBody>();
        Assert.NotNull(body);
        Assert.True(body!.Activo);
    }

    [Fact]
    public async Task ToggleModoAhorro_WhenAccessTokenHasStalePremiumClaimButDatabaseIsFree_Returns403()
    {
        using var client = _factory.CreateClient();
        var email = $"premium-gate-stale-claim-{Guid.NewGuid():N}@test.com";
        const string password = "Password123!";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, password, "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var user = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(user);

        await MakePremiumAsync(user!.HogarId);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var loginBody = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.NotNull(loginBody);
        Assert.Equal("premium", loginBody!.Plan);

        var stalePremiumToken = loginBody.AccessToken;
        await MakeFreeAsync(user.HogarId);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", stalePremiumToken);

        var response = await client.PatchAsJsonAsync("/api/finanzas/modo-ahorro", new { activo = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("PLAN_UPGRADE_REQUIRED", body!.Title);
    }

    private async Task<RegisterBody> AuthenticateAsync(HttpClient client, string prefix)
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

    private async Task MakePremiumAsync(Guid hogarId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var hogar = await db.Hogares.SingleAsync(x => x.Id == hogarId);
        hogar.Plan = "premium";
        hogar.SubscriptionStatus = "active";
        hogar.SuscripcionVenceEl = DateTime.UtcNow.AddDays(30);
        hogar.PlanUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task MakeFreeAsync(Guid hogarId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var hogar = await db.Hogares.SingleAsync(x => x.Id == hogarId);
        hogar.Plan = "free";
        hogar.SubscriptionStatus = "free";
        hogar.PlanUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record AuthBody(string AccessToken, string Plan, string SubscriptionStatus, DateTime? TrialEndsAt);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
    private sealed record ModoAhorroBody(bool Activo);
}
