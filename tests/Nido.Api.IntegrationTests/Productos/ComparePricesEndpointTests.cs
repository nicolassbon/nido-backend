using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Api.IntegrationTests.Auth;
using Nido.Application.Productos;
using Nido.Application.Productos.Exceptions;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Productos;

public sealed class ComparePricesEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public ComparePricesEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ComparePrices_WhenComparatorIsUnavailable_Returns503WithFrontendMessage()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPriceComparatorService>();
            services.AddSingleton<IPriceComparatorService>(new UnavailablePriceComparatorService());
        }));
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client);
        await MakePremiumAsync(factory, user.HogarId);

        var response = await client.GetAsync("/api/productos/comparar?q=leche");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorMessageBody>();
        Assert.NotNull(body);
        Assert.Equal("No pudimos comparar precios en este momento. Intentá nuevamente en unos minutos.", body!.Message);
    }

    [Fact]
    public async Task ComparePrices_WhenHouseholdIsFree_Returns403WithPlanUpgradeCode()
    {
        using var factory = WithSuccessfulComparator();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/productos/comparar?q=leche");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(body);
        Assert.Equal("PLAN_UPGRADE_REQUIRED", body!.Title);
    }

    [Fact]
    public async Task ComparePrices_WhenHouseholdIsPremium_ReturnsComparison()
    {
        using var factory = WithSuccessfulComparator();
        using var client = factory.CreateClient();
        var user = await AuthenticateAsync(client);
        await MakePremiumAsync(factory, user.HogarId);

        var response = await client.GetAsync("/api/productos/comparar?q=leche");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ComparePricesResult>();
        Assert.NotNull(body);
        var item = Assert.Single(body!.Products);
        Assert.Equal("Leche", item.Name);
    }

    private static WebApplicationFactory<Program> WithSuccessfulComparator() =>
        new NidoTestWebAppFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPriceComparatorService>();
            services.AddSingleton<IPriceComparatorService>(new SuccessfulPriceComparatorService());
        }));

    private static async Task<RegisterBody> AuthenticateAsync(HttpClient client)
    {
        var email = $"compare-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return body;
    }

    private static async Task MakePremiumAsync(WebApplicationFactory<Program> factory, Guid hogarId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var hogar = await db.Hogares.SingleAsync(x => x.Id == hogarId);
        hogar.Plan = "premium";
        hogar.SubscriptionStatus = "active";
        hogar.SuscripcionVenceEl = DateTime.UtcNow.AddDays(30);
        hogar.PlanUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private sealed class SuccessfulPriceComparatorService : IPriceComparatorService
    {
        public Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct)
            => Task.FromResult(new ComparePricesResult(
                [new ComparedProductDto("leche-1", "test", "Leche", "https://test.local/leche", "https://test.local/leche.png", 1500, "1 lt", 1500)],
                [],
                DateTime.UtcNow));
    }

    private sealed class UnavailablePriceComparatorService : IPriceComparatorService
    {
        public Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct)
            => throw new ComparatorUnavailableException(new HttpRequestException("connection refused"));
    }

    private sealed record RegisterBody(
        Guid UsuarioId,
        Guid HogarId,
        string AccessToken,
        string Plan,
        string SubscriptionStatus,
        DateTime? TrialEndsAt);

    private sealed record ErrorMessageBody(string Message);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
}
