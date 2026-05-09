using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Households;

public class CreateHouseholdPersistenceTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public CreateHouseholdPersistenceTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GivenAValidCreateRequest_WhenPostingHousehold_ThenPersistsRecordWithOnlyIdAndName()
    {
        // Given
        var request = new { name = "Casa de Nico" };

        // When
        var response = await _client.PostAsJsonAsync("/household", request);

        // Then
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var households = dbContext.Households.ToList();

        Assert.Single(households);
        Assert.NotEqual(Guid.Empty, households[0].Id);
        Assert.Equal("Casa de Nico", households[0].Name.Value);

        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA table_info('Household');";
        using var reader = command.ExecuteReader();
        var columnNames = new List<string>();
        while (reader.Read())
        {
            columnNames.Add(reader.GetString(1));
        }

        columnNames.Sort(StringComparer.Ordinal);

        Assert.Equal(["Id", "Name"], columnNames);
    }
}
