namespace Nido.Api.Contracts.Finanzas;

public sealed record BalanceMiembroResponse(
    Guid UsuarioId,
    string Nombre,
    string? FotoUrl,
    decimal MontoAportado,
    decimal MontoCorrespondido,
    decimal Balance
);

public sealed record DeudaResponse(
    Guid DeudorId,
    string DeudorNombre,
    string? DeudorFotoUrl,
    Guid AcreedorId,
    string AcreedorNombre,
    string? AcreedorFotoUrl,
    decimal Monto
);

public sealed record BalanceResponse(
    IReadOnlyList<BalanceMiembroResponse> Miembros,
    decimal TotalPeriodo,
    decimal TotalPersonal,
    IReadOnlyList<DeudaResponse> Deudas
);
