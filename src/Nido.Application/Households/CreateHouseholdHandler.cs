using Nido.Domain.Households;

namespace Nido.Application.Households;

public sealed class CreateHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;

    public CreateHouseholdHandler(IHouseholdRepository householdRepository)
    {
        _householdRepository = householdRepository;
    }

    public async Task<CreateHouseholdResult> Handle(CreateHouseholdCommand command, CancellationToken cancellationToken)
    {
        var householdName = HouseholdName.Create(command.Name);
        var household = Household.Create(householdName);

        await _householdRepository.SaveAsync(household, cancellationToken);

        return new CreateHouseholdResult(household.Id, household.Name.Value);
    }
}
