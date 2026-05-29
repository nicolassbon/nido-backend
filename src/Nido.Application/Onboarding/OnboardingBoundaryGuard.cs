namespace Nido.Application.Onboarding;

internal static class OnboardingBoundaryGuard
{
    public static void EnsureClientIdsMatchClaims(Guid claimUsuarioId, Guid claimHogarId, Guid? clientUsuarioId, Guid? clientHogarId)
    {
        if (clientUsuarioId.HasValue && clientUsuarioId.Value != claimUsuarioId)
        {
            throw new UnauthorizedAccessException("Invalid usuarioId boundary.");
        }

        if (clientHogarId.HasValue && clientHogarId.Value != claimHogarId)
        {
            throw new UnauthorizedAccessException("Invalid hogarId boundary.");
        }
    }
}
