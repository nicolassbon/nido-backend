namespace Nido.Api.Contracts.Auth;

public sealed record RefreshResponse(
    string AccessToken,
    string Plan,
    string SubscriptionStatus,
    DateTime? TrialEndsAt);
