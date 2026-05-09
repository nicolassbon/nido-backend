using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Households;

public class PostHouseholdTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public PostHouseholdTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GivenAValidName_WhenPostingHousehold_ThenReturnsCreatedWithIdAndName()
    {
        // Given
        var request = new { name = "Casa de Nico" };

        // When
        var response = await _client.PostAsJsonAsync("/household", request);

        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out var idProperty));
        Assert.True(Guid.TryParse(idProperty.GetString(), out _));
        Assert.Equal("Casa de Nico", body.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("{ }")]
    [InlineData("{ \"name\": \"\" }")]
    [InlineData("{ \"name\": \"   \" }")]
    public async Task GivenAnInvalidName_WhenPostingHousehold_ThenReturnsBadRequestAndDoesNotPersist(string payload)
    {
        // Given
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        // When
        var response = await _client.PostAsync("/household", content);

        // Then
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid household name", body.GetProperty("title").GetString());
        Assert.Equal((int)HttpStatusCode.BadRequest, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("detail", out _));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.Empty(dbContext.Households);
    }
}
