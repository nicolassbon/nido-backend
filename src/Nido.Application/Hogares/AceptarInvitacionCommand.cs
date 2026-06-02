namespace Nido.Application.Hogares;

public sealed record AceptarInvitacionCommand(
    Guid UsuarioId,
    string Token);
