namespace Nido.Api.Contracts.Finanzas;

public sealed record BalanceMiembroResponse(
    Guid UsuarioId,
    string Nombre,
    string? FotoUrl,
    decimal MontoAportado,
    decimal MontoCorrespondido,
    decimal Balance
);

public sealed record BalanceResponse(
    IReadOnlyList<BalanceMiembroResponse> Miembros,
    decimal TotalPeriodo
);
