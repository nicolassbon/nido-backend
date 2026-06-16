namespace Nido.Application.Hogares;

public sealed record InvitarIntegranteCommand(
    Guid UsuarioId,
    Guid HogarId,
    string EmailInvitado);
