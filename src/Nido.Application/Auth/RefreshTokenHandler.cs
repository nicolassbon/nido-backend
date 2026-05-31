using Nido.Application.Auth.Exceptions;

namespace Nido.Application.Auth;

public sealed class RefreshTokenHandler
{
    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenHandler(IAuthRepository repository, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
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

        var accessToken = _jwtTokenService.CreateToken(tokenInfo.UsuarioId, hogarId, user.Email, user.Nombre);

        return new RefreshTokenResult(accessToken);
    }
}
