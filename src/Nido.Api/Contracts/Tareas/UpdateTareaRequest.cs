namespace Nido.Api.Contracts.Tareas;

public sealed record UpdateTareaRequest(
    string? Titulo,
    string? Descripcion,
    DateTime? FechaLimite,
    string? Estado);
