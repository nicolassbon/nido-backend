using System.Text.RegularExpressions;

namespace Nido.Application.Auth.Helpers;

public static partial class PasswordRules
{
    public static bool IsValid(string password) => PasswordComplexityRegex().IsMatch(password);

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&.]{8,}$")]
    private static partial Regex PasswordComplexityRegex();
}
