namespace Nido.Application.Auth.RefreshToken;

public sealed record RefreshTokenResult(string AccessToken, string? RefreshToken = null);
