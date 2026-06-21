namespace Nido.Application.Hogares;

public sealed record GetHogaresQuery(Guid UsuarioId);

public sealed class GetHogaresHandler
{
    private readonly IHogarRepository _hogarRepository;

    public GetHogaresHandler(IHogarRepository hogarRepository)
    {
        _hogarRepository = hogarRepository;
    }

    public Task<List<HogarConRolInfo>> Handle(GetHogaresQuery query, CancellationToken ct)
        => _hogarRepository.GetUserHogaresAsync(query.UsuarioId, ct);
}
