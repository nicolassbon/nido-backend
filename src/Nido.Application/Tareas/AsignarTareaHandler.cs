namespace Nido.Application.Tareas;

public sealed record AsignarTareaCommand(Guid Id, Guid HogarId, Guid? AsignadoA, Guid AsignadoPor);

public sealed class AsignarTareaHandler(ITareaRepository repository)
{
    public Task<TareaResult?> Handle(AsignarTareaCommand command, CancellationToken ct) =>
        repository.AsignarAsync(command.Id, command.HogarId, command.AsignadoA, command.AsignadoPor, ct);
}
