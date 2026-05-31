using Nido.Application.Preferencias.Exceptions;

namespace Nido.Application.Preferencias;

public sealed class UpdateUserPreferencesHandler
{
    private readonly IUserPreferencesRepository _repository;

    public UpdateUserPreferencesHandler(IUserPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferencesResult> Handle(UpdateUserPreferencesCommand command, CancellationToken ct)
    {
        if (command.UsuarioId == Guid.Empty)
            throw new MissingPreferenceFieldException("usuario");

        if (command.DiasAlerta < 1 || command.DiasAlerta > 365)
            throw new InvalidPreferenceRangeException();

        return await _repository.UpdateAsync(command.UsuarioId, command.DiasAlerta, ct);
    }
}
