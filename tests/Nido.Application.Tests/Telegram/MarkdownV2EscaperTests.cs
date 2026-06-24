using Nido.Application.Telegram.Formatting;

namespace Nido.Application.Tests.Telegram;

public sealed class MarkdownV2EscaperTests
{
    [Fact]
    public void Escape_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MarkdownV2Escaper.Escape(null!));
    }

    [Fact]
    public void Escape_EmptyText_ReturnsEmptyString()
    {
        var result = MarkdownV2Escaper.Escape(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData('\\', "\\\\")]
    [InlineData('*', "\\*")]
    [InlineData('_', "\\_")]
    [InlineData('[', "\\[")]
    [InlineData(']', "\\]")]
    [InlineData('(', "\\(")]
    [InlineData(')', "\\)")]
    [InlineData('~', "\\~")]
    [InlineData('`', "\\`")]
    [InlineData('>', "\\>")]
    [InlineData('#', "\\#")]
    [InlineData('+', "\\+")]
    [InlineData('-', "\\-")]
    [InlineData('=', "\\=")]
    [InlineData('|', "\\|")]
    [InlineData('{', "\\{")]
    [InlineData('}', "\\}")]
    [InlineData('.', "\\.")]
    [InlineData('!', "\\!")]
    public void Escape_SingleReservedCharacter_AddsBackslashPrefix(char reserved, string expected)
    {
        var result = MarkdownV2Escaper.Escape(reserved.ToString());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_EveryReservedCharacterInOneString_EscapesEachOneOnce()
    {
        const string input = "\\_*[]()~`>#+-=|{}.!";
        const string expected = "\\\\\\_\\*\\[\\]\\(\\)\\~\\`\\>\\#\\+\\-\\=\\|\\{\\}\\.\\!";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_TextWithBackslashesAndReservedChars_EscapesBackslashAndReservedChars()
    {
        // Windows-style path mixed with MarkdownV2 reserved characters.
        const string input = "Ruta: C:\\Users\\nico (backup) - vence 2026-06-20. ¡OK!";
        const string expected = "Ruta: C:\\\\Users\\\\nico \\(backup\\) \\- vence 2026\\-06\\-20\\. ¡OK\\!";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_OnlyBackslashes_DoublesEachOne()
    {
        const string input = "\\\\\\";
        const string expected = "\\\\\\\\\\\\";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Yogur 2.0", "Yogur 2\\.0")]
    [InlineData("Pan (integral)", "Pan \\(integral\\)")]
    [InlineData("Leche - marca X", "Leche \\- marca X")]
    [InlineData("Hogar: ¡Queso!", "Hogar: ¡Queso\\!")]
    [InlineData("[Vencido] Yogur", "\\[Vencido\\] Yogur")]
    [InlineData("a + b = c", "a \\+ b \\= c")]
    [InlineData("#1: Manzana", "\\#1: Manzana")]
    [InlineData("100% natural", "100% natural")]
    [InlineData("café con leche", "café con leche")]
    [InlineData("Línea\nnueva", "Línea\nnueva")]
    [InlineData("uno\rdos", "uno\rdos")]
    [InlineData("tab\tseparado", "tab\tseparado")]
    public void Escape_RealWorldText_OnlyEscapesReservedCharacters(string input, string expected)
    {
        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_TextWithoutReservedCharacters_ReturnsIdenticalString()
    {
        const string input = "Hola mundo 123 abc";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Escape_TextWithNewlines_PreservesNewlines()
    {
        const string input = "Línea 1\nLínea 2\nLínea 3";
        const string expected = "Línea 1\nLínea 2\nLínea 3";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_TextWithSpaces_PreservesSpaces()
    {
        const string input = "uno  dos   tres";
        const string expected = "uno  dos   tres";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Escape_LongMessageWithMixedContent_EscapesOnlyReservedCharacters()
    {
        const string input = "[Stock bajo] Leche (marca X) - vence 2026-06-20. ¡Quedan 2!";
        const string expected = "\\[Stock bajo\\] Leche \\(marca X\\) \\- vence 2026\\-06\\-20\\. ¡Quedan 2\\!";

        var result = MarkdownV2Escaper.Escape(input);

        Assert.Equal(expected, result);
    }
}
