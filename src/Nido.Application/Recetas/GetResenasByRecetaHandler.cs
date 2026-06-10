namespace Nido.Application.Recetas;

public sealed record GetResenasByRecetaQuery(Guid RecetaId, Guid CallerUsuarioId);

public sealed record ResenasReceta(
    IReadOnlyList<ResenaItem> Items,
    ResenaResumen             Resumen,
    ResenaItem?               MiResena);

public sealed class GetResenasByRecetaHandler
{
    private readonly IResenaRecetaRepository _repository;

    public GetResenasByRecetaHandler(IResenaRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResenasReceta> Handle(GetResenasByRecetaQuery query, CancellationToken ct)
    {
        var items   = await _repository.GetByRecetaAsync(query.RecetaId, ct);
        var resumen = await _repository.GetResumenAsync(query.RecetaId, ct);
        var mia     = items.FirstOrDefault(r => r.UsuarioId == query.CallerUsuarioId);

        return new ResenasReceta(items, resumen, mia);
    }
}
