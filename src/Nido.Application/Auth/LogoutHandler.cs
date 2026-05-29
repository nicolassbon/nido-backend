namespace Nido.Application.Auth;

public sealed class LogoutHandler
{
    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;

    public LogoutHandler(IAuthRepository repository, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return;
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
        await _repository.RemoveRefreshTokenAsync(tokenHash, cancellationToken);
    }
}
