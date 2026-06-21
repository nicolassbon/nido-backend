namespace Nido.Application.Tareas;

public sealed record GetMisTareasQuery(Guid HogarId, Guid UsuarioId);

public sealed class GetMisTareasHandler(ITareaRepository repository)
{
    public Task<List<TareaResult>> Handle(GetMisTareasQuery query, CancellationToken ct) =>
        repository.GetByAsignadoAsync(query.HogarId, query.UsuarioId, ct);
}
