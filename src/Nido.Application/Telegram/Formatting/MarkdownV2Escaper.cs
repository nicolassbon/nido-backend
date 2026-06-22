using System.Text;

namespace Nido.Application.Telegram.Formatting;

public static class MarkdownV2Escaper
{
    private const string ReservedCharacters = "\\_*[]()~`>#+-=|{}.!";

    public static string Escape(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length + 8);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (IsReserved(c))
            {
                builder.Append('\\');
            }
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static bool IsReserved(char c)
    {
        return ReservedCharacters.IndexOf(c) >= 0;
    }
}
