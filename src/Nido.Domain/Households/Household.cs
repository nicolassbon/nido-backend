namespace Nido.Domain.Households;

public sealed class Household
{
    private Household()
    {
        Name = null!;
    }

    private Household(Guid id, HouseholdName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public HouseholdName Name { get; private set; }

    public static Household Create(HouseholdName name)
    {
        return new Household(Guid.NewGuid(), name);
    }
}
