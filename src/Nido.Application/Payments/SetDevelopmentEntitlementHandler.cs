using Nido.Application.Common.Security;

namespace Nido.Application.Payments;

public sealed class SetDevelopmentEntitlementHandler
{
    private static readonly TimeSpan PremiumFixtureDuration = TimeSpan.FromDays(30);
    private readonly IDevelopmentEntitlementRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;

    public SetDevelopmentEntitlementHandler(
        IDevelopmentEntitlementRepository repository,
        IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    public async Task<HouseholdEntitlement> Handle(
        string? plan,
        Guid usuarioId,
        Guid hogarId,
        CancellationToken ct)
    {
        var targetPlan = plan?.Trim().ToLowerInvariant() switch
        {
            "premium" => HouseholdPlan.Premium,
            "free" => HouseholdPlan.Free,
            _ => throw new ArgumentException("Plan must be either 'premium' or 'free'.", nameof(plan))
        };

        await _membershipService.EnsureMemberAsync(usuarioId, hogarId, ct);

        var nowUtc = DateTime.UtcNow;
        var subscriptionEndsAt = targetPlan == HouseholdPlan.Premium
            ? nowUtc.Add(PremiumFixtureDuration)
            : (DateTime?)null;

        return await _repository.SetAsync(hogarId, targetPlan, nowUtc, subscriptionEndsAt, ct);
    }
}
