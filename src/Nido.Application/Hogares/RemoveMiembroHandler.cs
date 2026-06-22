using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record RemoveMiembroCommand(Guid CallerUsuarioId, Guid HogarId, Guid TargetUsuarioId);

public sealed class RemoveMiembroHandler
{
    private readonly IInvitacionRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;

    public RemoveMiembroHandler(IInvitacionRepository repository, IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    public async Task Handle(RemoveMiembroCommand command, CancellationToken ct)
    {
        await _membershipService.EnsureOwnerAsync(command.CallerUsuarioId, command.HogarId, ct);

        if (command.CallerUsuarioId == command.TargetUsuarioId)
            throw new CannotRemoveSelfException();

        await _membershipService.EnsureMemberAsync(command.TargetUsuarioId, command.HogarId, ct);

        await _repository.RemoveMiembroAsync(command.HogarId, command.TargetUsuarioId, ct);
    }
}
