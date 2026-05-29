namespace Nido.Application.Auth;

public sealed class LinkGoogleHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IJwtTokenService _jwtTokenService;

    public LinkGoogleHandler(IAuthRepository repository, IPasswordHasher passwordHasher, IGoogleTokenValidator googleValidator, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _googleValidator = googleValidator;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LinkGoogleResult> Handle(LinkGoogleCommand command, CancellationToken cancellationToken)
    {
        GooglePayload payload;
        try
        {
            payload = await _googleValidator.ValidateAsync(command.IdToken, cancellationToken);
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("INVALID_GOOGLE_TOKEN");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.OauthProvider == "google" && !string.IsNullOrEmpty(user.OauthId))
        {
            throw new InvalidOperationException("ACCOUNT_ALREADY_LINKED");
        }

        await _repository.UpdateUserAsync(
            new User(user.Id, normalizedEmail, user.PasswordHash, "google", payload.GoogleId),
            cancellationToken);

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("User has no associated household.");

        var (accessToken, refreshToken) = _jwtTokenService.CreateAuthTokens(user.Id, hogarId, normalizedEmail);
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        await _repository.AddRefreshTokenAsync(user.Id, refreshTokenHash, expiresAt, cancellationToken);

        return new LinkGoogleResult(user.Id, hogarId, accessToken, refreshToken);
    }
}
