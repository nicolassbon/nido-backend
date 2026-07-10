using Nido.Application.Payments;

namespace Nido.Application.Auth.RefreshToken;

public sealed record RefreshTokenResult(
    string AccessToken,
    string? RefreshToken = null,
    HouseholdPlan Plan = HouseholdPlan.Free,
    SubscriptionStatus SubscriptionStatus = SubscriptionStatus.None,
    DateTime? TrialEndsAt = null);
