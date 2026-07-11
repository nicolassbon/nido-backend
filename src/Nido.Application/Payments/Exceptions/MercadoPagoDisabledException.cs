using Nido.Domain.Exceptions;

namespace Nido.Application.Payments.Exceptions;

public sealed class MercadoPagoDisabledException : NidoException
{
    public MercadoPagoDisabledException()
        : base("MERCADO_PAGO_DISABLED", "Mercado Pago payments are disabled in this environment.")
    {
    }
}
