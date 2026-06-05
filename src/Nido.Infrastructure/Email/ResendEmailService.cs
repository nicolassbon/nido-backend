using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nido.Application.Common.Notifications;
using Resend;

namespace Nido.Infrastructure.Email;

public sealed class ResendEmailService : IEmailService
{
    private static readonly HtmlEncoder HtmlEncoder = HtmlEncoder.Default;

    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly IResend _resend;

    public ResendEmailService(
        IConfiguration configuration,
        ILogger<ResendEmailService> logger,
        IResend resend)
    {
        _configuration = configuration;
        _logger = logger;
        _resend = resend;
    }

    public async Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var invitationLink = $"{frontendBaseUrl}/invitacion?token={Uri.EscapeDataString(invitationToken)}";

        var textBody = $"""
            Hola,

            {invitadoPorNombre} te invitó a unirse al hogar "{hogarNombre}" en Nido.

            Para aceptar la invitación, hacé click en el siguiente link:
            {invitationLink}

            Este link expira en 7 días.

            Si no esperabas esta invitación, podés ignorar este mensaje.

            ¡Hasta pronto!
            El equipo de Nido
            """;

        var htmlBody = BuildHtmlBody(
            $"{invitadoPorNombre} te invitó a unirse al hogar \"{hogarNombre}\" en Nido.",
            $"<p><a href=\"{HtmlEncoder.Encode(invitationLink)}\">Aceptar invitación</a></p>",
            "Este link expira en 7 días.",
            "Si no esperabas esta invitación, podés ignorar este mensaje.",
            "¡Hasta pronto!\nEl equipo de Nido");

        await SendEmailAsync(
            toEmail,
            $"{invitadoPorNombre} te invitó a unirse a su hogar en Nido",
            textBody,
            htmlBody,
            ct,
            debugMessage: $"[EMAIL][INVITATION] To: {toEmail} | Hogar: {hogarNombre} | Token: {invitationToken}");
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var resetLink = $"{frontendBaseUrl}/restablecer-contrasena?token={Uri.EscapeDataString(resetToken)}";

        var textBody = $"""
            Hola,

            Recibimos una solicitud para restablecer tu contraseña.

            Usá este enlace para crear una nueva contraseña:
            {resetLink}

            Si no solicitaste este cambio, podés ignorar este correo.

            El equipo de Nido
            """;

        var htmlBody = BuildHtmlBody(
            "Recibimos una solicitud para restablecer tu contraseña.",
            $"<p><a href=\"{HtmlEncoder.Encode(resetLink)}\">Restablecer contraseña</a></p>",
            "Si no solicitaste este cambio, podés ignorar este correo.",
            "El equipo de Nido");

        await SendEmailAsync(
            toEmail,
            "Restablecé tu contraseña en Nido",
            textBody,
            htmlBody,
            ct,
            debugMessage: $"[EMAIL][RESET] To: {toEmail} | Password reset email queued (token redacted)");
    }

    public async Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct)
    {
        const string textBody = """
            Hola,

            Tu cuenta está configurada para ingresar con Google.

            Iniciá sesión con Google y luego, desde configuración de seguridad,
            podés crear una contraseña para habilitar también acceso por email.

            El equipo de Nido
            """;

        var htmlBody = BuildHtmlBody(
            "Tu cuenta está configurada para ingresar con Google.",
            "Iniciá sesión con Google y luego, desde configuración de seguridad, podés crear una contraseña para habilitar también acceso por email.",
            "El equipo de Nido");

        await SendEmailAsync(
            toEmail,
            "Tu cuenta usa acceso con Google",
            textBody,
            htmlBody,
            ct,
            debugMessage: $"[EMAIL][GOOGLE-ONLY] To: {toEmail}");
    }

    public async Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct)
    {
        const string textBody = """
            Hola,

            Alguien intentó registrarse en Nido usando este correo electrónico.

            Si fuiste vos, podés iniciar sesión normalmente con tu cuenta.
            Si no reconocés este intento, te recomendamos cambiar tu contraseña o revisar tu acceso.

            El equipo de Nido
            """;

        var htmlBody = BuildHtmlBody(
            "Alguien intentó registrarse en Nido usando este correo electrónico.",
            "Si fuiste vos, podés iniciar sesión normalmente con tu cuenta.",
            "Si no reconocés este intento, te recomendamos cambiar tu contraseña o revisar tu acceso.",
            "El equipo de Nido");

        await SendEmailAsync(
            toEmail,
            "Intentaron registrarse con tu correo en Nido",
            textBody,
            htmlBody,
            ct,
            debugMessage: $"[EMAIL][DUPLICATE-SIGNUP] To: {toEmail}");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody, CancellationToken ct, string debugMessage)
    {
        var apiKey = _configuration["RESEND_API_KEY"];
        var fromAddress = _configuration["Email:FromAddress"] ?? "onboarding@resend.dev";
        var fromName = _configuration["Email:FromName"] ?? "Nido";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(debugMessage);
                return;
            }

            throw new InvalidOperationException("Missing required configuration: RESEND_API_KEY");
        }

        var message = new EmailMessage
        {
            From = FormatFrom(fromName, fromAddress),
            Subject = subject,
            TextBody = textBody,
            HtmlBody = htmlBody
        };

        message.To.Add(toEmail);

        try
        {
            _logger.LogInformation("Sending email via Resend to {Recipient} with subject {Subject}", toEmail, subject);
            await _resend.EmailSendAsync(message, ct);
            _logger.LogInformation("Resend email sent to {Recipient} with subject {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend send failed to {Recipient} with subject {Subject}", toEmail, subject);
            throw;
        }
    }

    internal static string FormatFrom(string? fromName, string fromAddress)
    {
        return string.IsNullOrWhiteSpace(fromName)
            ? fromAddress
            : $"{fromName} <{fromAddress}>";
    }

    internal static string BuildHtmlBody(params string[] paragraphs)
    {
        var renderedParagraphs = paragraphs
            .Where(static paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .Select(RenderParagraph);

        return $"<html><body>{string.Join(string.Empty, renderedParagraphs)}</body></html>";
    }

    private static string RenderParagraph(string paragraph)
    {
        var trimmed = paragraph.Trim();
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var encoded = HtmlEncoder.Encode(trimmed)
            .Replace("\n", "<br/>", StringComparison.Ordinal);

        return $"<p>{encoded}</p>";
    }
}
