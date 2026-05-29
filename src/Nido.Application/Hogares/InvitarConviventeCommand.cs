namespace Nido.Application.Hogares;

public sealed record InvitarConviventeCommand(
    Guid UsuarioId,
    Guid HogarId,
    string EmailInvitado);
