using Nido.Application.Common.Security;

namespace Nido.Application.Tareas;

public sealed record AsignarTareaCommand(Guid Id, Guid HogarId, Guid? AsignadoA, Guid AsignadoPor);

public sealed class AsignarTareaHandler(
    ITareaRepository repository,
    IHouseholdMembershipService membershipService)
{
    public async Task<TareaResult?> Handle(AsignarTareaCommand command, CancellationToken ct)
    {
        if (command.AsignadoA.HasValue)
        {
            await membershipService.EnsureMemberAsync(
                command.AsignadoA.Value, command.HogarId, ct);
        }

        return await repository.AsignarAsync(
            command.Id, command.HogarId, command.AsignadoA, command.AsignadoPor, ct);
    }
}
