namespace Nido.Application.Planificador;

public sealed record PlanificadorItemResult(
    Guid     Id,
    DateOnly Fecha,
    string   TipoComida,
    Guid?    RecetaId,
    string?  RecetaNombre,
    string?  ImagenUrl,
    string?  TituloLibre,
    string?  Hora,
    int      Orden,
    Guid     CreadoPor);

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
    string?  Hora);

public sealed record DeletePlanificadorItemCommand(
    Guid ItemId,
    Guid HogarId);
