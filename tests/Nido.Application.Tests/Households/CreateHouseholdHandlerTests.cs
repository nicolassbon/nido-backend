using Nido.Application.Households;
using Nido.Domain.Households;

namespace Nido.Application.Tests.Households;

public class CreateHouseholdHandlerTests
{
    [Fact]
    public async Task GivenAValidName_WhenHandlingCreateHousehold_ThenSavesExactlyOneHousehold()
    {
        // Given
        var repository = new InMemoryHouseholdRepository();
        var handler = new CreateHouseholdHandler(repository);
        var command = new CreateHouseholdCommand("Casa de Nico");

        // When
        var result = await handler.Handle(command, CancellationToken.None);

        // Then
        Assert.Single(repository.SavedHouseholds);
        var savedHousehold = repository.SavedHouseholds[0];
        Assert.Equal(savedHousehold.Id, result.Id);
        Assert.Equal("Casa de Nico", result.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenAnInvalidName_WhenHandlingCreateHousehold_ThenDoesNotSave(string invalidName)
    {
        // Given
        var repository = new InMemoryHouseholdRepository();
        var handler = new CreateHouseholdHandler(repository);
        var command = new CreateHouseholdCommand(invalidName);

        // When
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Then
        await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Empty(repository.SavedHouseholds);
    }

    private sealed class InMemoryHouseholdRepository : IHouseholdRepository
    {
        public List<Household> SavedHouseholds { get; } = [];

        public Task SaveAsync(Household household, CancellationToken cancellationToken)
        {
            SavedHouseholds.Add(household);
            return Task.CompletedTask;
        }
    }
}
