using Nido.Application.Payments;

namespace Nido.Application.Auth.Google.Login;

public sealed record GoogleLoginResult(
    Guid UsuarioId,
    Guid HogarId,
    string AccessToken,
    bool IsNewUser,
    string? RefreshToken = null,
    HouseholdPlan Plan = HouseholdPlan.Free,
    SubscriptionStatus SubscriptionStatus = SubscriptionStatus.None,
    DateTime? TrialEndsAt = null);
