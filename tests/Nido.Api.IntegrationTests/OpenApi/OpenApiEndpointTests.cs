using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Nido.Api.IntegrationTests.OpenApi;

public class OpenApiEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public OpenApiEndpointTests(NidoTestWebAppFactory factory)
    {
        // We configure the client to follow redirects (if any) and target the test server.
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });
    }

    [Fact]
    public async Task GetOpenApiJson_ReturnsSuccessAndValidDocument()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Assert content is JSON
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

        // Verify it contains OpenAPI spec structure
        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();
        Assert.NotNull(document);
        Assert.NotNull(document["openapi"]);
        Assert.Equal("3.1.1", document["openapi"]?.ToString());
        Assert.NotNull(document["info"]);
        Assert.Equal("Nido.Api | v1", document["info"]?["title"]?.ToString());
    }

    [Fact]
    public async Task GetOpenApiJson_DescribesPushSubscriptionEndpointContract()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();

        var operation = document?["paths"]?["/api/notificaciones/suscripciones"]?["post"];

        Assert.NotNull(operation);
        Assert.Equal("true", operation?["requestBody"]?["required"]?.ToString());
        Assert.NotNull(operation?["responses"]?["204"]);
        Assert.NotNull(operation?["responses"]?["400"]);
        Assert.NotNull(operation?["security"]);
    }
}
