namespace Nido.Application.Finanzas;

public sealed record DeleteGastoResult(bool FacturaRevertida, Guid? FacturaId);
