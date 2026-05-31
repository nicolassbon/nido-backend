namespace Nido.Application.Auth;

public static class AuthTokenHelper
{
    public static async Task<(string AccessToken, string RefreshToken)> CreateAndPersistRefreshTokenAsync(
        IJwtTokenService jwtTokenService,
        IAuthRepository repository,
        Guid usuarioId,
        Guid hogarId,
        string email,
        string nombre,
        CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken, expiresAt) = jwtTokenService.CreateAuthTokens(usuarioId, hogarId, email, nombre);
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);

        await repository.AddRefreshTokenAsync(usuarioId, refreshTokenHash, expiresAt, cancellationToken);

        return (accessToken, refreshToken);
    }
}
