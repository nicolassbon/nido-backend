namespace Nido.Application.Auth.ChangePassword;

public sealed record ChangePasswordCommand(Guid UsuarioId, string CurrentPassword, string NewPassword, string NewPasswordConfirmation);
