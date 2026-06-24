namespace Nido.Application.Planificador;

public sealed record PlanificadorItemResult(
    Guid     Id,
    DateOnly Fecha,
    string   TipoComida,
    Guid?    TareaId,
    Guid?    RecetaId,
    string?  RecetaNombre,
    string?  ImagenUrl,
    string?  TituloLibre,
    string?  Hora,
    string?  TareaEstado,
    PlanificadorAsignacionResult? AsignadoA,
    int      Orden,
    Guid     CreadoPor);

public sealed record PlanificadorAsignacionResult(
    Guid UsuarioId,
    string Nombre,
    string? FotoStorageKey);

public sealed record PlanificadorSemanaResult(
    Guid                            Id,
    DateOnly                        FechaInicio,
    IReadOnlyList<PlanificadorItemResult> Items);

public sealed record AddPlanificadorItemCommand(
    Guid     HogarId,
    Guid     UsuarioId,
    DateOnly Fecha,
    string   TipoComida,
    Guid?    RecetaId,
    string?  TituloLibre,
    string?  Hora,
    Guid?    AsignadoA);

public sealed record UpdatePlanificadorItemCommand(
    Guid    ItemId,
    Guid    HogarId,
    Guid    UsuarioId,
    Guid?   RecetaId,
    string? TituloLibre,
    string? Hora,
    Guid?   AsignadoA);

public sealed record DeletePlanificadorItemCommand(
    Guid ItemId,
    Guid HogarId);
