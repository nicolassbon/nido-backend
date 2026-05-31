using Nido.Domain.Exceptions;

namespace Nido.Application.Onboarding.Exceptions;

public sealed class BoundaryViolationException : NidoException
{
    public BoundaryViolationException(string message) : base("BOUNDARY_VIOLATION", message) { }
}

public sealed class HouseholdAccessDeniedException : NidoException
{
    public HouseholdAccessDeniedException() : base("HOUSEHOLD_ACCESS_DENIED", "User does not belong to household.") { }
}
