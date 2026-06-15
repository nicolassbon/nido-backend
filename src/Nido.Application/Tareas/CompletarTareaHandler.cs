namespace Nido.Application.Tareas;

public sealed record CompletarTareaCommand(Guid Id, Guid HogarId, Guid CompletadoPor);

public sealed class CompletarTareaHandler(ITareaRepository repository)
{
    public Task<TareaResult?> Handle(CompletarTareaCommand command, CancellationToken ct) =>
        repository.CompletarAsync(command.Id, command.HogarId, command.CompletadoPor, ct);
}
