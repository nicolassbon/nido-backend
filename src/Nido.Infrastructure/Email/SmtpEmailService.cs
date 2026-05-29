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
}
