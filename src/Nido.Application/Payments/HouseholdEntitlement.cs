namespace Nido.Application.Payments;

public sealed record HouseholdEntitlement(
    HouseholdPlan Plan,
    SubscriptionStatus SubscriptionStatus,
    DateTime? TrialEndsAt,
    DateTime? SubscriptionEndsAt = null)
{
    public HouseholdEntitlement GetEffective(DateTime utcNow)
    {
        if (TrialEndsAt.HasValue && TrialEndsAt.Value > utcNow)
        {
            return this with { Plan = HouseholdPlan.Premium, SubscriptionStatus = SubscriptionStatus.Active };
        }

        if (Plan != HouseholdPlan.Premium || SubscriptionStatus != SubscriptionStatus.Active)
        {
            return this;
        }

        if (SubscriptionEndsAt.HasValue && SubscriptionEndsAt.Value > utcNow)
        {
            return this;
        }

        return this with
        {
            Plan = HouseholdPlan.Free,
            SubscriptionStatus = SubscriptionStatus.None,
            TrialEndsAt = null,
            SubscriptionEndsAt = null
        };
    }
}
