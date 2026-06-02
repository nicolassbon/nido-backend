using Nido.Domain.Exceptions;

namespace Nido.Application.Auth.Exceptions;

public sealed class InvalidCredentialsException : NidoException
{
    public InvalidCredentialsException() : base("INVALID_CREDENTIALS", "Invalid email or password") { }
}

public sealed class LoginCredentialsMissingException : NidoException
{
    public LoginCredentialsMissingException() : base("LOGIN_CREDENTIALS_MISSING", "Email and password are required.") { }
}

public sealed class MissingRegistrationFieldsException : NidoException
{
    public MissingRegistrationFieldsException(IEnumerable<string> fields)
        : base("MISSING_REGISTRATION_FIELDS", $"Missing required fields: {string.Join(", ", fields)}") { }
}

public sealed class WeakPasswordException : NidoException
{
    public WeakPasswordException() : base("WEAK_PASSWORD", "Password does not meet complexity requirements.") { }
}

public sealed class InvalidGoogleTokenException : NidoException
{
    public InvalidGoogleTokenException(string code, string? message = null)
        : base(code, message ?? "Invalid Google token.") { }
}

public sealed class EmailAlreadyExistsException : NidoException
{
    public EmailAlreadyExistsException() : base("EMAIL_ALREADY_EXISTS", "Email already exists.") { }
}

public sealed class AccountAlreadyLinkedException : NidoException
{
    public AccountAlreadyLinkedException(string code) : base(code, "Account already linked.") { }
}

public sealed class UserNotFoundException : NidoException
{
    public UserNotFoundException() : base("USER_NOT_FOUND", "User not found.") { }
}

public sealed class UserNotInHouseholdException : NidoException
{
    public UserNotInHouseholdException() : base("USER_NOT_IN_HOUSEHOLD", "User has no associated household.") { }
}

public sealed class InvalidRefreshTokenException : NidoException
{
    public InvalidRefreshTokenException(string code, string? message = null)
        : base(code, message ?? "Invalid refresh token.") { }
}

public sealed class NoHouseholdAssociatedException : NidoException
{
    public NoHouseholdAssociatedException() : base("NO_HOUSEHOLD_ASSOCIATED", "User has no associated household.") { }
}

public sealed class InvalidResetTokenException : NidoException
{
    public InvalidResetTokenException() : base("INVALID_RESET_TOKEN", "Reset token is invalid or expired.") { }
}

public sealed class InvalidPasswordException : NidoException
{
    public InvalidPasswordException(string code, string message) : base(code, message) { }
}

public sealed class PasswordAlreadySetException : NidoException
{
    public PasswordAlreadySetException() : base("PASSWORD_ALREADY_SET", "This account already has a password.") { }
}
