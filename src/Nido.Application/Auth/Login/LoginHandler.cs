using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;

namespace Nido.Application.Auth.Login;

public sealed class LoginHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginHandler(IAuthRepository repository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            throw new LoginCredentialsMissingException();
        }

        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.Equals(user.OauthProvider, "google", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(user.OauthId))
            {
                throw new AccountLinkRequiredException(
                    "ACCOUNT_EXISTS_WITH_GOOGLE",
                    "This account was created with Google. Use Google login or set a password from account linking.");
            }

            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new UserNotInHouseholdException();

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            user.Id,
            hogarId,
            normalizedEmail,
            user.Nombre,
            cancellationToken);

        return new LoginResult(user.Id, hogarId, accessToken, refreshToken);
    }
}
