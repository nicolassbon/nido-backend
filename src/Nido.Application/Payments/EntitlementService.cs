using Nido.Application.Payments.Exceptions;

namespace Nido.Application.Payments;

public sealed class EntitlementService : IEntitlementService
{
    private readonly IEntitlementRepository _repository;
    public EntitlementService(IEntitlementRepository repository)
    {
        _repository = repository;
    }

    public async Task EnsurePremiumAsync(Guid hogarId, CancellationToken ct)
    {
        var entitlement = await GetAsync(hogarId, ct);

        if (entitlement.Plan == HouseholdPlan.Premium)
        {
            return;
        }

        throw new PremiumRequiredException();
    }

    public async Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct)
    {
        var entitlement = await _repository.GetAsync(hogarId, ct);
        return entitlement.GetEffective(DateTime.UtcNow);
    }
}
