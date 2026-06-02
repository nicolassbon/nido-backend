namespace Nido.Application.Auth.RefreshToken;

public sealed record RefreshTokenInfo(
    Guid Id,
    Guid UsuarioId,
    string TokenHash,
    DateTime ExpiresAt);
