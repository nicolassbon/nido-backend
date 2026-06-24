using Nido.Application.Common.Security;

namespace Nido.Application.Tareas;

public sealed record CreateTareaCommand(
    Guid HogarId,
    Guid CreadoPor,
    string Titulo,
    string? Descripcion,
    DateTime? FechaLimite,
    Guid? AsignadoA);

public sealed class CreateTareaHandler(
    ITareaRepository repository,
    IHouseholdMembershipService membershipService)
{
    public async Task<TareaResult> Handle(CreateTareaCommand command, CancellationToken ct)
    {
        if (command.AsignadoA.HasValue)
        {
            await membershipService.EnsureMemberAsync(
                command.AsignadoA.Value, command.HogarId, ct);
        }

        return await repository.CreateAsync(
            command.HogarId,
            command.CreadoPor,
            command.Titulo,
            command.Descripcion,
            command.FechaLimite,
            command.AsignadoA,
            ct);
    }
}
