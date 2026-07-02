using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Auth.Google.Login;

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
        catch (Exception)
        {
            throw new InvalidGoogleTokenException("INVALID_GOOGLE_TOKEN");
        }

        var normalizedEmail = EmailNormalizer.Normalize(payload.Email);

        var user = await ResolveUserAsync(payload, normalizedEmail, cancellationToken);
        ValidateAccountLinkingConflict(user);

        var (usuarioId, hogarId, isNewUser) = await ResolveLoginDataAsync(user, payload, normalizedEmail, cancellationToken);

        var nombre = user?.Nombre ?? ResolveDisplayName(payload, normalizedEmail);

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            usuarioId,
            hogarId,
            user?.Email ?? normalizedEmail,
            nombre,
            cancellationToken);

        return new GoogleLoginResult(usuarioId, hogarId, accessToken, isNewUser, refreshToken);
    }

    private async Task<User?> ResolveUserAsync(GooglePayload payload, string normalizedEmail, CancellationToken cancellationToken)
    {
        var user = await _repository.FindByGoogleIdAsync(payload.GoogleId, cancellationToken);
        if (user is not null) return user;

        user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null
            && string.Equals(user.OauthProvider, "google", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(user.OauthId)
            && user.OauthId != payload.GoogleId)
        {
            throw new InvalidGoogleTokenException("GOOGLE_ACCOUNT_MISMATCH", "Google account mismatch.");
        }

        return user;
    }

    private static void ValidateAccountLinkingConflict(User? user)
    {
        if (user is not null && !string.IsNullOrEmpty(user.PasswordHash) && string.IsNullOrEmpty(user.OauthId))
        {
            // TODO: Send email to user with instructions to link their Google account from settings
            throw new AccountLinkRequiredException(
                "ACCOUNT_EXISTS_WITH_PASSWORD",
                "This account uses password. Link Google from settings or use password login.");
        }
    }

    private async Task<(Guid UsuarioId, Guid HogarId, bool IsNewUser)> ResolveLoginDataAsync(
        User? user, GooglePayload payload, string normalizedEmail, CancellationToken cancellationToken)
    {
        if (user is null)
        {
            var newUserData = new CreateOAuthUserData(
                UsuarioId: Guid.NewGuid(),
                HogarId: Guid.NewGuid(),
                Nombre: ResolveDisplayName(payload, normalizedEmail),
                Email: normalizedEmail,
                OauthProvider: "google",
                OauthId: payload.GoogleId,
                FotoStorageKey: ResolvePictureUrl(payload));

            var (newUsuarioId, newHogarId) = await _repository.CreateUserWithGoogleAsync(newUserData, cancellationToken);
            return (newUsuarioId, newHogarId, IsNewUser: true);
        }

        var hogarId = await _repository.GetUserHogarIdAsync(user.Id, cancellationToken)
            ?? throw new UserNotInHouseholdException();

        return (user.Id, hogarId, IsNewUser: false);
    }

    private static string ResolveDisplayName(GooglePayload payload, string normalizedEmail)
    {
        if (!string.IsNullOrWhiteSpace(payload.Name))
        {
            return payload.Name.Trim();
        }

        return normalizedEmail.Split('@')[0];
    }

    private static string? ResolvePictureUrl(GooglePayload payload)
        => ProfileImageReferenceRules.NormalizeExternalUrlOrNull(payload.Picture);
}
