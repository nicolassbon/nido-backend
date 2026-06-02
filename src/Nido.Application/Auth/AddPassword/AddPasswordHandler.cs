using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;

namespace Nido.Application.Auth.AddPassword;

public sealed class AddPasswordHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public AddPasswordHandler(IAuthRepository repository, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AddPasswordResult> Handle(AddPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.FindByIdAsync(command.UsuarioId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new PasswordAlreadySetException();
        }

        if (command.NewPassword != command.NewPasswordConfirmation)
        {
            throw new InvalidPasswordException("PASSWORD_CONFIRMATION_MISMATCH", "Password confirmation does not match.");
        }

        if (!PasswordRules.IsValid(command.NewPassword))
        {
            throw new WeakPasswordException();
        }

        var passwordHash = _passwordHasher.Hash(command.NewPassword);
        await _repository.UpdateUserPasswordAsync(command.UsuarioId, passwordHash, cancellationToken);

        return new AddPasswordResult();
    }
}
