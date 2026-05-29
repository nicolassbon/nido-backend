namespace Nido.Application.Hogares;

public sealed class GetMiembrosHandler
{
    private readonly IInvitacionRepository _repository;

    public GetMiembrosHandler(IInvitacionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MiembroInfo>> Handle(GetMiembrosQuery query, CancellationToken ct)
    {
        if (!await _repository.IsUserInAnyHouseholdAsync(query.UsuarioId, ct))
            throw new UnauthorizedAccessException("El usuario no pertenece a ningún hogar.");

        return await _repository.GetMiembrosAsync(query.HogarId, ct);
    }
}
