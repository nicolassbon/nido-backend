using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record GetHogarQuery(Guid CallerUsuarioId, Guid HogarId);

public sealed class GetHogarHandler
{
    private readonly IHogarRepository _hogarRepository;
    private readonly IInvitacionRepository _invitacionRepository;

    public GetHogarHandler(IHogarRepository hogarRepository, IInvitacionRepository invitacionRepository)
    {
        _hogarRepository = hogarRepository;
        _invitacionRepository = invitacionRepository;
    }

    public async Task<HogarInfo> Handle(GetHogarQuery query, CancellationToken ct)
    {
        if (!await _invitacionRepository.IsMemberOfHouseholdAsync(query.CallerUsuarioId, query.HogarId, ct))
            throw new NotHouseholdMemberException();

        var hogar = await _hogarRepository.GetByIdAsync(query.HogarId, ct);
        if (hogar is null)
            throw new NotHouseholdMemberException();

        return hogar;
    }
}
