using Nido.Application.Payments;

namespace Nido.Application.Auth.Login;

public sealed record LoginResult(
    Guid UsuarioId,
    Guid HogarId,
    string AccessToken,
    string? RefreshToken = null,
    HouseholdPlan Plan = HouseholdPlan.Free,
    SubscriptionStatus SubscriptionStatus = SubscriptionStatus.None,
    DateTime? TrialEndsAt = null);
