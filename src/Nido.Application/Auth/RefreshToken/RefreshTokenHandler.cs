using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Payments;

namespace Nido.Application.Auth.RefreshToken;

public sealed class RefreshTokenHandler
{
    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEntitlementService _entitlementService;

    public RefreshTokenHandler(IAuthRepository repository, IJwtTokenService jwtTokenService, IEntitlementService entitlementService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
        _entitlementService = entitlementService;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            throw new InvalidRefreshTokenException("MISSING_REFRESH_TOKEN");
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
        var tokenInfo = await _repository.GetValidRefreshTokenAsync(tokenHash, cancellationToken);

        if (tokenInfo is null)
        {
            throw new InvalidRefreshTokenException("INVALID_REFRESH_TOKEN");
        }

        var hogarId = await _repository.GetUserHogarIdAsync(tokenInfo.UsuarioId, cancellationToken)
            ?? throw new UserNotInHouseholdException();

        var user = await _repository.FindByIdAsync(tokenInfo.UsuarioId, cancellationToken)
            ?? throw new UserNotFoundException();

        var entitlement = await _entitlementService.GetAsync(hogarId, cancellationToken);
        var accessToken = _jwtTokenService.CreateToken(
            tokenInfo.UsuarioId,
            hogarId,
            user.Email,
            user.Nombre,
            entitlement);

        return new RefreshTokenResult(
            accessToken,
            null,
            entitlement.Plan,
            entitlement.SubscriptionStatus,
            entitlement.TrialEndsAt);
    }
}
