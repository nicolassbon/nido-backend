namespace Nido.Application.Tareas;

public sealed record GetDistribucionSemanalQuery(Guid HogarId, int UtcOffsetMinutes);

public sealed class GetDistribucionSemanalHandler(ITareaRepository repository)
{
    public Task<List<DistribucionDiaResult>> Handle(GetDistribucionSemanalQuery query, CancellationToken ct) =>
        repository.GetDistribucionSemanalAsync(query.HogarId, query.UtcOffsetMinutes, ct);
}
