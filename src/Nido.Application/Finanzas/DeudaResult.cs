namespace Nido.Application.Finanzas;

public sealed record DeudaResult(
    Guid DeudorId,
    string DeudorNombre,
    string? DeudorFotoUrl,
    Guid AcreedorId,
    string AcreedorNombre,
    string? AcreedorFotoUrl,
    decimal Monto
);
