namespace Nido.Application.Finanzas;

public sealed record PagarFacturaResult(FacturaResult Factura, GastoResult? GastoCreado);
