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

        if (command.DiasAlerta is < 1 or > 365)
            throw new InvalidPreferenceRangeException();

        if (command.TemaPreferido is not null && !UserThemeModes.IsValid(command.TemaPreferido))
            throw new InvalidThemeModeException();

        return await _repository.UpdateAsync(command.UsuarioId, command.DiasAlerta, command.TemaPreferido, ct);
    }
}
