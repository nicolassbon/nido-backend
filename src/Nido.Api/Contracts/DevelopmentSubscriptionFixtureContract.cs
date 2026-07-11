namespace Nido.Api.Contracts;

public sealed record DevelopmentSubscriptionFixtureRequest(string? Plan);

public sealed record DevelopmentSubscriptionFixtureResponse(
    string Plan,
    string SubscriptionStatus,
    DateTime? SubscriptionEndsAt);
