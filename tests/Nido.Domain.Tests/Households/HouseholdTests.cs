using Nido.Domain.Households;

namespace Nido.Domain.Tests.Households;

public class HouseholdTests
{
    [Fact]
    public void GivenAValidHouseholdName_WhenCreatingHousehold_ThenExposesIdAndName()
    {
        // Given
        var householdName = HouseholdName.Create("Casa de Nico");

        // When
        var household = Household.Create(householdName);

        // Then
        Assert.NotEqual(Guid.Empty, household.Id);
        Assert.Equal("Casa de Nico", household.Name.Value);
    }

    [Fact]
    public void GivenTwoCreateCalls_WhenCreatingHouseholds_ThenEachHouseholdHasDistinctId()
    {
        // Given
        var householdName = HouseholdName.Create("Casa de Nico");

        // When
        var firstHousehold = Household.Create(householdName);
        var secondHousehold = Household.Create(householdName);

        // Then
        Assert.NotEqual(firstHousehold.Id, secondHousehold.Id);
    }
}
