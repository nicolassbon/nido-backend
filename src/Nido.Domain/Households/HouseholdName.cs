namespace Nido.Domain.Households;

public sealed class HouseholdName
{
    private HouseholdName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static HouseholdName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Household name is required.", nameof(name));
        }

        return new HouseholdName(name.Trim());
    }
}
