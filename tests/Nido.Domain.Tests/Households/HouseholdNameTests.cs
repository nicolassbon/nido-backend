using Nido.Domain.Households;

namespace Nido.Domain.Tests.Households;

public class HouseholdNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenAnInvalidName_WhenCreatingHouseholdName_ThenThrowsArgumentException(string? rawName)
    {
        // Given
        var candidateName = rawName;

        // When
        Action act = () => HouseholdName.Create(candidateName!);

        // Then
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenAValidName_WhenCreatingHouseholdName_ThenStoresValue()
    {
        // Given
        const string validName = "Casa de Nico";

        // When
        var householdName = HouseholdName.Create(validName);

        // Then
        Assert.Equal(validName, householdName.Value);
    }
}
