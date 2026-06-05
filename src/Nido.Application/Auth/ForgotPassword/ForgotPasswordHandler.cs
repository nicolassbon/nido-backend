using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.Notifications;

namespace Nido.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
{
    private const int ResetTokenExpiryMinutes = 60;
    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;

    public ForgotPasswordHandler(
        IAuthRepository repository,
        IJwtTokenService jwtTokenService,
        IEmailService emailService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return new ForgotPasswordResult();
        }

        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var user = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return new ForgotPasswordResult();
        }

        var hasGoogleLinked = string.Equals(user.OauthProvider, "google", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(user.OauthId);

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            var rawToken = _jwtTokenService.GenerateRefreshToken();
            var tokenHash = _jwtTokenService.HashRefreshToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes);

            await _repository.SavePasswordResetTokenAsync(user.Id, tokenHash, expiresAt, cancellationToken);
            await _emailService.SendPasswordResetEmailAsync(normalizedEmail, rawToken, cancellationToken);

            return new ForgotPasswordResult();
        }

        if (hasGoogleLinked)
        {
            await _emailService.SendGoogleOnlyInfoEmailAsync(normalizedEmail, cancellationToken);
        }

        return new ForgotPasswordResult();
    }
}
