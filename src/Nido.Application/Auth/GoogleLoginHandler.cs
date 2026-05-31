namespace Nido.Application.Auth;

public sealed class GoogleLoginHandler
{
    private readonly IAuthRepository _repository;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IJwtTokenService _jwtTokenService;

    public GoogleLoginHandler(
        IAuthRepository repository,
        IGoogleTokenValidator googleTokenValidator,
        IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _googleTokenValidator = googleTokenValidator;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<GoogleLoginResult> Handle(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        GooglePayload payload;
        try
        {
            payload = await _googleTokenValidator.ValidateAsync(command.IdToken, cancellationToken);
        }
        catch
        {
            throw new UnauthorizedAccessException("INVALID_GOOGLE_TOKEN");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await _repository.FindByGoogleIdAsync(payload.GoogleId, cancellationToken);

        if (user is null)
        {
            user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

            if (user is not null
                && string.Equals(user.OauthProvider, "google", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(user.OauthId)
                && user.OauthId != payload.GoogleId)
            {
                throw new UnauthorizedAccessException("GOOGLE_ACCOUNT_MISMATCH");
            }
        }

        if (user is not null && !string.IsNullOrEmpty(user.PasswordHash) && string.IsNullOrEmpty(user.OauthId))
        {
            throw new AccountLinkRequiredException("ACCOUNT_EXISTS_WITH_PASSWORD", "This account uses password. Link Google from settings or use password login.");
        }

        bool isNewUser;
        Guid usuarioId;
        Guid hogarId;

        if (user is null)
        {
            var (newUsuarioId, newHogarId) = await _repository.CreateUserWithDefaultHouseholdAsync(
                normalizedEmail.Split('@')[0],
                normalizedEmail,
                string.Empty,
                "U",
                null,
                cancellationToken,
                oauthProvider: "google",
                oauthId: payload.GoogleId);

            usuarioId = newUsuarioId;
            hogarId = newHogarId;
            isNewUser = true;
        }
        else
        {
            usuarioId = user.Id;
            hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
                ?? throw new InvalidOperationException("User has no associated household.");
            isNewUser = false;
        }

        var nombreFinal = user?.Nombre ?? normalizedEmail.Split('@')[0];
        var (accessToken, refreshToken) = _jwtTokenService.CreateAuthTokens(usuarioId, hogarId, user?.Email ?? normalizedEmail, nombreFinal);
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        await _repository.AddRefreshTokenAsync(usuarioId, refreshTokenHash, expiresAt, cancellationToken);

        return new GoogleLoginResult(usuarioId, hogarId, accessToken, isNewUser, refreshToken);
    }
}
