namespace Nido.Application.Preferencias;

public sealed class GetUserPreferencesHandler
{
    private readonly IUserPreferencesRepository _repository;

    public GetUserPreferencesHandler(IUserPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferencesResult> Handle(GetUserPreferencesQuery query, CancellationToken ct)
    {
        if (query.UsuarioId == Guid.Empty)
            throw new ArgumentException("El usuario es requerido.");

        return await _repository.GetByUsuarioAsync(query.UsuarioId, ct);
    }
}
