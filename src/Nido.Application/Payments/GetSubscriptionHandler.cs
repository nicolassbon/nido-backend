namespace Nido.Application.Payments;

public sealed class GetSubscriptionHandler
{
    private readonly IEntitlementService _entitlementService;

    public GetSubscriptionHandler(IEntitlementService entitlementService)
    {
        _entitlementService = entitlementService;
    }

    public async Task<GetSubscriptionResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var entitlement = await _entitlementService.GetAsync(hogarId, ct);
        return new GetSubscriptionResult(
            entitlement.Plan.ToResponseString(),
            entitlement.SubscriptionStatus.ToResponseString(entitlement.Plan),
            entitlement.TrialEndsAt,
            entitlement.SubscriptionEndsAt);
    }
}

public sealed record GetSubscriptionResult(string Plan, string SubscriptionStatus, DateTime? TrialEndsAt, DateTime? SubscriptionEndsAt);
