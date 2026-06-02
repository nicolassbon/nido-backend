namespace Nido.Application.Auth.AddPassword;

public sealed record AddPasswordCommand(Guid UsuarioId, string NewPassword, string NewPasswordConfirmation);
