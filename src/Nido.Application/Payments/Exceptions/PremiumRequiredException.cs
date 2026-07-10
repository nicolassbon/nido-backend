using Nido.Domain.Exceptions;

namespace Nido.Application.Payments.Exceptions;

public sealed class PremiumRequiredException : NidoException
{
    public PremiumRequiredException()
        : base("PLAN_UPGRADE_REQUIRED", "Esta función requiere un plan Premium.")
    {
    }
}
