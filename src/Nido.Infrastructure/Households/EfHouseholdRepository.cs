using Nido.Application.Households;
using Nido.Domain.Households;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Households;

public sealed class EfHouseholdRepository : IHouseholdRepository
{
    private readonly NidoDbContext _dbContext;

    public EfHouseholdRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(Household household, CancellationToken cancellationToken)
    {
        _dbContext.Households.Add(household);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
