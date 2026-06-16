namespace Nido.Application.Tareas;

public sealed record CreateTareaCommand(
    Guid HogarId,
    Guid CreadoPor,
    string Titulo,
    string? Descripcion,
    DateTime? FechaLimite,
    Guid? AsignadoA);

public sealed class CreateTareaHandler(ITareaRepository repository)
{
    public Task<TareaResult> Handle(CreateTareaCommand command, CancellationToken ct) =>
        repository.CreateAsync(
            command.HogarId,
            command.CreadoPor,
            command.Titulo,
            command.Descripcion,
            command.FechaLimite,
            command.AsignadoA,
            ct);
}
