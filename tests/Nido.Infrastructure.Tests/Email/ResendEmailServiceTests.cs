using Nido.Infrastructure.Email;

namespace Nido.Infrastructure.Tests.Email;

public sealed class ResendEmailServiceTests
{
    [Fact]
    public void FormatFrom_WithName_ReturnsDisplayNameFormat()
    {
        var result = ResendEmailService.FormatFrom("Nido", "onboarding@resend.dev");

        Assert.Equal("Nido <onboarding@resend.dev>", result);
    }

    [Fact]
    public void FormatFrom_WithoutName_ReturnsAddressOnly()
    {
        var result = ResendEmailService.FormatFrom(" ", "onboarding@resend.dev");

        Assert.Equal("onboarding@resend.dev", result);
    }

    [Fact]
    public void BuildHtmlBody_EncodesPlainTextAndPreservesMarkupParagraphs()
    {
        var result = ResendEmailService.BuildHtmlBody(
            "Hola <Nido>",
            "<p><a href=\"https://example.com\">Aceptar invitación</a></p>");

        Assert.Equal(
            "<html><body><p>Hola &lt;Nido&gt;</p><p><a href=\"https://example.com\">Aceptar invitación</a></p></body></html>",
            result);
    }
}
