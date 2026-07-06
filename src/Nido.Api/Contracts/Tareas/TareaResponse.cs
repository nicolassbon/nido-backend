namespace Nido.Api.Contracts.Tareas;

public sealed record AsignacionResponse(Guid UsuarioId, string Nombre, string? FotoStorageKey);

public sealed record TareaResponse(
    Guid Id,
    string Titulo,
    string? Descripcion,
    string Estado,
    DateTime? FechaLimite,
    DateTime? FechaCompletado,
    Guid CreadoPor,
    string CreadoPorNombre,
    Guid? CompletadoPor,
    string? CompletadoPorNombre,
    AsignacionResponse? AsignadoA,
    bool Vencida,
    DateTime CreatedAt,
    int? XpOtorgado);
