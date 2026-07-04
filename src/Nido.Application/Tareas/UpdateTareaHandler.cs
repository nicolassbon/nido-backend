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

    public async Task<TareaResult?> Handle(UpdateTareaCommand command, CancellationToken ct)
    {
        // Reject PATCH to "completada" — only POST /api/tareas/{id}/completar can complete
        if (command.Estado == "completada")
        {
            throw new InvalidOperationException(
                "Use POST /api/tareas/{id}/completar para completar una tarea. " +
                "El estado 'completada' no se puede asignar mediante PATCH.");
        }

        return await repository.UpdateAsync(
            command.Id,
            command.HogarId,
            command.Titulo,
            command.Descripcion,
            command.FechaLimite,
            command.Estado,
            ct);
    }
}
