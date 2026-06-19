namespace Nido.Api.Contracts.Tareas;

public sealed record CreateTareaRequest(
    string Titulo,
    string? Descripcion,
    DateTime? FechaLimite,
    Guid? AsignadoA);
