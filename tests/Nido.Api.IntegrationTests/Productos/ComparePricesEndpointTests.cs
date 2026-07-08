using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Api.IntegrationTests.Auth;
using Nido.Application.Productos;
using Nido.Application.Productos.Exceptions;

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
        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/productos/comparar?q=leche");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorMessageBody>();
        Assert.NotNull(body);
        Assert.Equal("No pudimos comparar precios en este momento. Intentá nuevamente en unos minutos.", body!.Message);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"compare-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private sealed class UnavailablePriceComparatorService : IPriceComparatorService
    {
        public Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct)
            => throw new ComparatorUnavailableException(new HttpRequestException("connection refused"));
    }

    private sealed record RegisterBody(Guid? UsuarioId, Guid? HogarId, string AccessToken);

    private sealed record ErrorMessageBody(string Message);
}
