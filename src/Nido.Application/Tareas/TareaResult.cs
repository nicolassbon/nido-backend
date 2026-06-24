namespace Nido.Application.Tareas;

public sealed record AsignacionResult(Guid UsuarioId, string Nombre, string? FotoStorageKey);

public sealed record TareaResult(
    Guid Id,
    Guid HogarId,
    string Titulo,
    string? Descripcion,
    string Estado,
    DateTime? FechaLimite,
    DateTime? FechaCompletado,
    Guid CreadoPor,
    string CreadoPorNombre,
    Guid? CompletadoPor,
    string? CompletadoPorNombre,
    AsignacionResult? AsignadoA,
    DateTime CreatedAt);
