namespace Nido.Application.Auth;

public sealed record RefreshTokenResult(string AccessToken, string? RefreshToken = null);
