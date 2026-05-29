namespace Nido.Application.Auth;

public sealed record RefreshTokenInfo(
    Guid Id,
    Guid UsuarioId,
    string TokenHash,
    DateTime ExpiresAt);
