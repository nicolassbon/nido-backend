using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Onboarding;

internal static class OnboardingBoundaryGuard
{
    public static void EnsureClientIdsMatchClaims(Guid claimUsuarioId, Guid claimHogarId, Guid? clientUsuarioId, Guid? clientHogarId)
    {
        if (clientUsuarioId.HasValue && clientUsuarioId.Value != claimUsuarioId)
        {
            throw new BoundaryViolationException("Invalid usuarioId boundary.");
        }

        if (clientHogarId.HasValue && clientHogarId.Value != claimHogarId)
        {
            throw new BoundaryViolationException("Invalid hogarId boundary.");
        }
    }
}
