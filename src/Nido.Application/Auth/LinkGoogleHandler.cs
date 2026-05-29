namespace Nido.Application.Auth;

public sealed class LinkGoogleHandler
{
    private readonly IAuthRepository _repository;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IJwtTokenService _jwtTokenService;

    public LinkGoogleHandler(IAuthRepository repository, IGoogleTokenValidator googleValidator, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
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

        var user = await _repository.FindByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var normalizedUserEmail = user.Email.Trim().ToLowerInvariant();
        var normalizedGoogleEmail = payload.Email.Trim().ToLowerInvariant();
        if (normalizedUserEmail != normalizedGoogleEmail)
        {
            throw new UnauthorizedAccessException("GOOGLE_EMAIL_MISMATCH");
        }

        var linkedUser = await _repository.FindByGoogleIdAsync(payload.GoogleId, cancellationToken);
        if (linkedUser is not null && linkedUser.Id != user.Id)
        {
            throw new InvalidOperationException("GOOGLE_ACCOUNT_ALREADY_LINKED");
        }

        if (user.OauthProvider == "google" && !string.IsNullOrEmpty(user.OauthId))
        {
            throw new InvalidOperationException("ACCOUNT_ALREADY_LINKED");
        }

        await _repository.UpdateUserAsync(
            new User(user.Id, user.Email, user.PasswordHash, "google", payload.GoogleId),
            cancellationToken);

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("User has no associated household.");

        var (accessToken, refreshToken) = _jwtTokenService.CreateAuthTokens(user.Id, hogarId, user.Email);
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        await _repository.AddRefreshTokenAsync(user.Id, refreshTokenHash, expiresAt, cancellationToken);

        return new LinkGoogleResult(user.Id, hogarId, accessToken, refreshToken);
    }
}
