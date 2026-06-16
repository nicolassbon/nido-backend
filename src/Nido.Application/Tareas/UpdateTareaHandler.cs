namespace Nido.Application.Tareas;

public sealed record UpdateTareaCommand(
    Guid Id,
    Guid HogarId,
    string? Titulo,
    string? Descripcion,
    DateTime? FechaLimite,
    string? Estado);

public sealed class UpdateTareaHandler(ITareaRepository repository)
{
    public Task<TareaResult?> Handle(UpdateTareaCommand command, CancellationToken ct) =>
        repository.UpdateAsync(
            command.Id,
            command.HogarId,
            command.Titulo,
            command.Descripcion,
            command.FechaLimite,
            command.Estado,
            ct);
}
