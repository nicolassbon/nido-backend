namespace Nido.Application.Auth;

public sealed record User(
    Guid Id,
    string Nombre,
    string Email,
    string? PasswordHash,
    string? OauthProvider,
    string? OauthId);
