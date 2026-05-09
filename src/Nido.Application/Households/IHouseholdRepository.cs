using Nido.Domain.Households;

namespace Nido.Application.Households;

public interface IHouseholdRepository
{
    Task SaveAsync(Household household, CancellationToken cancellationToken);
}
