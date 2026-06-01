namespace Nido.Application.Auth.ResetPassword;

public sealed record PasswordResetTokenInfo(Guid Id, Guid UsuarioId, string TokenHash, DateTime ExpiresAt, DateTime? UsedAt);
