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
            throw new ArgumentException("El usuario es requerido.");

        if (command.DiasAlerta < 1 || command.DiasAlerta > 365)
            throw new ArgumentException("Los días de alerta deben estar entre 1 y 365.");

        return await _repository.UpdateAsync(command.UsuarioId, command.DiasAlerta, ct);
    }
}
