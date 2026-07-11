namespace Nido.Application.Payments;

public interface IDevelopmentEntitlementRepository
{
    Task<HouseholdEntitlement> SetAsync(
        Guid hogarId,
        HouseholdPlan plan,
        DateTime nowUtc,
        DateTime? subscriptionEndsAt,
        CancellationToken ct);
}
