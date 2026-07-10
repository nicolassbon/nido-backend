using Microsoft.EntityFrameworkCore;
using Nido.Application.Payments;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Payments;

public sealed class EntitlementRepository : IEntitlementRepository
{
    private readonly NidoDbContext _context;

    public EntitlementRepository(NidoDbContext context)
    {
        _context = context;
    }

    public async Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct)
    {
        var hogar = await _context.Hogares
            .AsNoTracking()
            .Where(h => h.Id == hogarId)
            .Select(h => new
            {
                h.Plan,
                h.SubscriptionStatus,
                h.TrialEndsAt,
                h.SuscripcionVenceEl
            })
            .FirstOrDefaultAsync(ct);

        if (hogar is null)
        {
            throw new InvalidOperationException($"Household {hogarId} not found.");
        }

        return new HouseholdEntitlement(
            ParsePlan(hogar.Plan),
            ParseSubscriptionStatus(hogar.SubscriptionStatus),
            hogar.TrialEndsAt.HasValue ? hogar.TrialEndsAt.Value.ToUniversalTime() : null,
            hogar.SuscripcionVenceEl.HasValue ? hogar.SuscripcionVenceEl.Value.ToUniversalTime() : null);
    }

    private static HouseholdPlan ParsePlan(string value) => value.ToLowerInvariant() switch
    {
        "premium" => HouseholdPlan.Premium,
        _ => HouseholdPlan.Free
    };

    private static SubscriptionStatus ParseSubscriptionStatus(string value) => value.ToLowerInvariant() switch
    {
        "pending" => SubscriptionStatus.Pending,
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "cancelled" => SubscriptionStatus.Cancelled,
        _ => SubscriptionStatus.None
    };
}
