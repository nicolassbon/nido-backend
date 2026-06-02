using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;

namespace Nido.Application.Auth.ResetPassword;

public sealed class ResetPasswordHandler
{
    private readonly IAuthRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(IAuthRepository repository, IJwtTokenService jwtTokenService, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            throw new InvalidResetTokenException();
        }

        if (command.NewPassword != command.NewPasswordConfirmation)
        {
            throw new InvalidPasswordException("PASSWORD_CONFIRMATION_MISMATCH", "Password confirmation does not match.");
        }

        if (!PasswordRules.IsValid(command.NewPassword))
        {
            throw new WeakPasswordException();
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(command.Token);
        var tokenInfo = await _repository.GetValidPasswordResetTokenAsync(tokenHash, cancellationToken);
        if (tokenInfo is null)
        {
            throw new InvalidResetTokenException();
        }

        var passwordHash = _passwordHasher.Hash(command.NewPassword);
        await _repository.UpdateUserPasswordAsync(tokenInfo.UsuarioId, passwordHash, cancellationToken);
        await _repository.ConsumePasswordResetTokenAsync(tokenHash, cancellationToken);

        return new ResetPasswordResult();
    }
}
