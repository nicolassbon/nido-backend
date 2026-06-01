namespace Nido.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword, string NewPasswordConfirmation);
