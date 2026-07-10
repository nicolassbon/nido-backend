namespace Nido.Application.Payments;

public interface IEntitlementService
{
    Task EnsurePremiumAsync(Guid hogarId, CancellationToken ct);
    Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct);
}
