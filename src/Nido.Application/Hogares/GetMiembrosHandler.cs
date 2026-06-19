using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed class GetMiembrosHandler
{
    private readonly IInvitacionRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;

    public GetMiembrosHandler(IInvitacionRepository repository, IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    public async Task<List<MiembroInfo>> Handle(GetMiembrosQuery query, CancellationToken ct)
    {
        await _membershipService.EnsureMemberAsync(query.UsuarioId, query.HogarId, ct);

        return await _repository.GetMiembrosAsync(query.HogarId, ct);
    }
}
