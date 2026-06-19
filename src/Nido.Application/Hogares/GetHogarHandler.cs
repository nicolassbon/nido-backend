using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record GetHogarQuery(Guid CallerUsuarioId, Guid HogarId);

public sealed class GetHogarHandler
{
    private readonly IHogarRepository _hogarRepository;
    private readonly IHouseholdMembershipService _membershipService;

    public GetHogarHandler(IHogarRepository hogarRepository, IHouseholdMembershipService membershipService)
    {
        _hogarRepository = hogarRepository;
        _membershipService = membershipService;
    }

    public async Task<HogarInfo> Handle(GetHogarQuery query, CancellationToken ct)
    {
        await _membershipService.EnsureMemberAsync(query.CallerUsuarioId, query.HogarId, ct);

        var hogar = await _hogarRepository.GetByIdAsync(query.HogarId, ct);
        if (hogar is null)
            throw new NotHouseholdMemberException();

        return hogar;
    }
}
