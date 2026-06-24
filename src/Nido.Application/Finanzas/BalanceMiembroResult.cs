namespace Nido.Application.Finanzas;

public sealed record BalanceMiembroResult(
    Guid UsuarioId,
    string Nombre,
    string? FotoUrl,
    decimal MontoAportado,
    decimal MontoCorrespondido,
    decimal Balance
);
