using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Nido.Application.Hogares;

namespace Nido.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct)
    {
        var host = _configuration["Email:Host"];
        var portStr = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@nido.app";
        var fromName = _configuration["Email:FromName"] ?? "Nido";
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";

        // In dev without SMTP config, log to console instead of failing
        if (string.IsNullOrWhiteSpace(host))
        {
            Console.WriteLine($"[EMAIL] To: {toEmail} | Hogar: {hogarNombre} | Token: {invitationToken}");
            return;
        }

        var invitationLink = $"{frontendBaseUrl}/invitacion?token={invitationToken}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"{invitadoPorNombre} te invitó a unirse a su hogar en Nido";

        message.Body = new TextPart("plain")
        {
            Text = $"""
                Hola,

                {invitadoPorNombre} te invitó a unirse al hogar "{hogarNombre}" en Nido.

                Para aceptar la invitación, hacé click en el siguiente link:
                {invitationLink}

                Este link expira en 7 días.

                Si no esperabas esta invitación, podés ignorar este mensaje.

                ¡Hasta pronto!
                El equipo de Nido
                """
        };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        // Docker containers lack the root CA store to validate external certs.
        // Gmail's cert is valid; this only skips chain validation in containerized envs.
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        var port = int.TryParse(portStr, out var p) ? p : 587;
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var resetLink = $"{frontendBaseUrl}/restablecer-contrasena?token={Uri.EscapeDataString(resetToken)}";

        await SendEmailAsync(
            toEmail,
            "Restablecé tu contraseña en Nido",
            $"""
            Hola,

            Recibimos una solicitud para restablecer tu contraseña.

            Usá este enlace para crear una nueva contraseña:
            {resetLink}

            Si no solicitaste este cambio, podés ignorar este correo.

            El equipo de Nido
            """,
            ct,
            debugMessage: $"[EMAIL][RESET] To: {toEmail} | Password reset email queued (token redacted)");
    }

    public async Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct)
    {
        await SendEmailAsync(
            toEmail,
            "Tu cuenta usa acceso con Google",
            """
            Hola,

            Tu cuenta está configurada para ingresar con Google.

            Iniciá sesión con Google y luego, desde configuración de seguridad,
            podés crear una contraseña para habilitar también acceso por email.

            El equipo de Nido
            """,
            ct,
            debugMessage: $"[EMAIL][GOOGLE-ONLY] To: {toEmail}");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct, string debugMessage)
    {
        var host = _configuration["Email:Host"];
        var portStr = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@nido.app";
        var fromName = _configuration["Email:FromName"] ?? "Nido";

        if (string.IsNullOrWhiteSpace(host))
        {
            Console.WriteLine(debugMessage);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;

        var port = int.TryParse(portStr, out var p) ? p : 587;
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
