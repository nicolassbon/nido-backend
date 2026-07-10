using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Api.IntegrationTests.Auth;
using Nido.Application.Productos;

namespace Nido.Api.IntegrationTests.Productos;

public sealed class ExternalLookupEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public ExternalLookupEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(HttpClient client)
    {
        var email = $"lookup-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    [Fact]
    public async Task ExternalLookup_WithValidBarcode_Returns200AndExpectedShape()
    {
        var stub = new StubLookupService(new LookupExternalProductoResult(
            Name:              "Leche entera La Serenísima",
            Image:             "https://images.example/leche.jpg",
            Brands:            "La Serenísima",
            CategoriesTags:    new[] { "en:dairies", "en:milks" },
            CategoriaSugerida: "Lácteos",
            FoundInDb:         true));

        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IExternalProductLookupService));
            services.AddSingleton<IExternalProductLookupService>(stub);
        }));

        using var client = await CreateAuthenticatedClientAsync(factory.CreateClient());

        var response = await client.GetAsync("/api/productos/external-lookup/7791234567890");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ExternalLookupResponseShape>();
        Assert.NotNull(payload);
        Assert.Equal("Leche entera La Serenísima", payload!.Name);
        Assert.Equal("Lácteos", payload.CategoriaSugerida);
        Assert.True(payload.FoundInDb);
        Assert.Equal(new[] { "en:dairies", "en:milks" }, payload.CategoriesTags);

        Assert.Equal("7791234567890", stub.LastBarcode);
    }

    [Theory]
    [InlineData("abc")]            // no numérico
    [InlineData("1234567")]        // < 8 dígitos
    [InlineData("123456789012345")] // > 14 dígitos
    public async Task ExternalLookup_WithInvalidBarcode_Returns400(string invalid)
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IExternalProductLookupService));
            services.AddSingleton<IExternalProductLookupService>(new StubLookupService(EmptyResult()));
        }));

        using var client = await CreateAuthenticatedClientAsync(factory.CreateClient());

        var response = await client.GetAsync($"/api/productos/external-lookup/{invalid}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static LookupExternalProductoResult EmptyResult() =>
        new(string.Empty, string.Empty, string.Empty, Array.Empty<string>(), "General", false);

    private sealed class StubLookupService : IExternalProductLookupService
    {
        private readonly LookupExternalProductoResult _result;
        public string? LastBarcode { get; private set; }

        public StubLookupService(LookupExternalProductoResult result) => _result = result;

        public Task<LookupExternalProductoResult> LookupAsync(string barcode, CancellationToken ct)
        {
            LastBarcode = barcode;
            return Task.FromResult(_result);
        }
    }

    private sealed record RegisterBody(
        Guid? UsuarioId,
        Guid? HogarId,
        string AccessToken,
        string Plan,
        string SubscriptionStatus,
        DateTime? TrialEndsAt);

    private sealed class ExternalLookupResponseShape
    {
        public string   Name              { get; set; } = string.Empty;
        public string   Image             { get; set; } = string.Empty;
        public string   Brands            { get; set; } = string.Empty;
        public string[] CategoriesTags    { get; set; } = Array.Empty<string>();
        public string   CategoriaSugerida { get; set; } = string.Empty;
        public bool     FoundInDb         { get; set; }
    }
}
