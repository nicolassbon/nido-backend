namespace Nido.Application.Auth;

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
            throw new ArgumentException("Email and password are required.");
        }

        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
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

            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("User has no associated household.");

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            user.Id,
            hogarId,
            normalizedEmail,
            cancellationToken);

        return new LoginResult(user.Id, hogarId, accessToken, refreshToken);
    }
}
