namespace Nido.Application.Tareas;

public sealed record GetTareasQuery(Guid HogarId);

public sealed class GetTareasHandler(ITareaRepository repository)
{
    public Task<List<TareaResult>> Handle(GetTareasQuery query, CancellationToken ct) =>
        repository.GetByHogarAsync(query.HogarId, ct);
}
