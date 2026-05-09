using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Nido.Api.IntegrationTests;

public class HelloEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HelloEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHello_ReturnsOkWithWelcomeMessage()
    {
        // Given
        // The WebApplicationFactory is already set up

        // When
        var response = await _client.GetAsync("/hello");

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bienvenido a Nido!", body);
    }

    [Fact]
    public async Task GetHello_ReturnsJsonContentType()
    {
        // Given
        // The WebApplicationFactory is already set up

        // When
        var response = await _client.GetAsync("/hello");

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
