namespace Nido.Application.Hogares;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct);
}
