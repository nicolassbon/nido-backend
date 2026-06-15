namespace Nido.Application.Tareas;

public sealed record DeleteTareaCommand(Guid Id, Guid HogarId);

public sealed class DeleteTareaHandler(ITareaRepository repository)
{
    public Task<bool> Handle(DeleteTareaCommand command, CancellationToken ct) =>
        repository.DeleteAsync(command.Id, command.HogarId, ct);
}
