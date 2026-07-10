namespace Nido.Api.Contracts.Auth;

public sealed record RegisterResponse(
    Guid? UsuarioId,
    Guid? HogarId,
    string? AccessToken,
    string Message,
    bool IsSilentSuccess,
    string? Plan = null,
    string? SubscriptionStatus = null,
    DateTime? TrialEndsAt = null)
{
    public static RegisterResponse Created(
        Guid usuarioId,
        Guid hogarId,
        string accessToken,
        string plan,
        string subscriptionStatus,
        DateTime? trialEndsAt)
        => new(usuarioId, hogarId, accessToken, "Registration completed.", false, plan, subscriptionStatus, trialEndsAt);

    public static RegisterResponse SilentSuccess()
        => new(null, null, null, "If an account already exists for that email, we've kept the response generic for security.", true);
}
