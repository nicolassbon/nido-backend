namespace Nido.Application.Payments;

public interface IEntitlementRepository
{
    Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct);
}
