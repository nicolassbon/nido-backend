using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Payments;

namespace Nido.Application.Auth.Helpers;

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
        => await CreateAndPersistRefreshTokenAsync(
            jwtTokenService,
            repository,
            usuarioId,
            hogarId,
            email,
            nombre,
            new HouseholdEntitlement(HouseholdPlan.Free, SubscriptionStatus.None, null),
            cancellationToken);

    public static async Task<(string AccessToken, string RefreshToken)> CreateAndPersistRefreshTokenAsync(
        IJwtTokenService jwtTokenService,
        IAuthRepository repository,
        Guid usuarioId,
        Guid hogarId,
        string email,
        string nombre,
        HouseholdPlan plan,
        CancellationToken cancellationToken)
        => await CreateAndPersistRefreshTokenAsync(
            jwtTokenService,
            repository,
            usuarioId,
            hogarId,
            email,
            nombre,
            new HouseholdEntitlement(plan, SubscriptionStatus.None, null),
            cancellationToken);

    public static async Task<(string AccessToken, string RefreshToken)> CreateAndPersistRefreshTokenAsync(
        IJwtTokenService jwtTokenService,
        IAuthRepository repository,
        Guid usuarioId,
        Guid hogarId,
        string email,
        string nombre,
        HouseholdEntitlement entitlement,
        CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken, expiresAt) = jwtTokenService.CreateAuthTokens(usuarioId, hogarId, email, nombre, entitlement);
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);

        await repository.AddRefreshTokenAsync(usuarioId, refreshTokenHash, expiresAt, cancellationToken);

        return (accessToken, refreshToken);
    }
}
