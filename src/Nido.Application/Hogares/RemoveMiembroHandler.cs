using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record RemoveMiembroCommand(Guid CallerUsuarioId, Guid HogarId, Guid TargetUsuarioId);

public sealed class RemoveMiembroHandler
{
    private readonly IInvitacionRepository _repository;

    public RemoveMiembroHandler(IInvitacionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveMiembroCommand command, CancellationToken ct)
    {
        if (!await _repository.IsUserHouseholdOwnerAsync(command.CallerUsuarioId, command.HogarId, ct))
            throw new NotHouseholdOwnerException();

        if (command.CallerUsuarioId == command.TargetUsuarioId)
            throw new CannotRemoveSelfException();

        if (!await _repository.IsMemberOfHouseholdAsync(command.TargetUsuarioId, command.HogarId, ct))
            throw new NotHouseholdMemberException();

        await _repository.RemoveMiembroAsync(command.HogarId, command.TargetUsuarioId, ct);
    }
}
