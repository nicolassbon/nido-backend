namespace Nido.Application.Common.Notifications;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct);
    Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct);
    Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct);
}
