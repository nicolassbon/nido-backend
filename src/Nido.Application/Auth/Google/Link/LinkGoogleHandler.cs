using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Google.Login;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;

namespace Nido.Application.Auth.Google.Link;

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
            throw new InvalidGoogleTokenException("INVALID_GOOGLE_TOKEN");
        }

        var user = await _repository.FindByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            throw new UserNotFoundException();
        }

        var normalizedUserEmail = EmailNormalizer.Normalize(user.Email);
        var normalizedGoogleEmail = EmailNormalizer.Normalize(payload.Email);
        if (normalizedUserEmail != normalizedGoogleEmail)
        {
            throw new InvalidGoogleTokenException("GOOGLE_EMAIL_MISMATCH", "Google email mismatch.");
        }

        var linkedUser = await _repository.FindByGoogleIdAsync(payload.GoogleId, cancellationToken);
        if (linkedUser is not null && linkedUser.Id != user.Id)
        {
            throw new AccountAlreadyLinkedException("GOOGLE_ACCOUNT_ALREADY_LINKED");
        }

        if (user.OauthProvider == "google" && !string.IsNullOrEmpty(user.OauthId))
        {
            throw new AccountAlreadyLinkedException("ACCOUNT_ALREADY_LINKED");
        }

        await _repository.UpdateUserAsync(
            user with { OauthProvider = "google", OauthId = payload.GoogleId },
            cancellationToken);

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new NoHouseholdAssociatedException();

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            user.Id,
            hogarId,
            user.Email,
            user.Nombre,
            cancellationToken);

        return new LinkGoogleResult(user.Id, hogarId, accessToken, refreshToken);
    }
}
