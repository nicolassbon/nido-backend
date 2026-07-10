using Nido.Application.Payments;

namespace Nido.Application.Auth.Google.Link;

public sealed record LinkGoogleResult(
    Guid UsuarioId,
    Guid HogarId,
    string AccessToken,
    string? RefreshToken = null,
    HouseholdPlan Plan = HouseholdPlan.Free,
    SubscriptionStatus SubscriptionStatus = SubscriptionStatus.None,
    DateTime? TrialEndsAt = null);
