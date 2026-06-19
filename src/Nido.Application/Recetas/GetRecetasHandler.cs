namespace Nido.Application.Recetas;

public sealed class GetRecetasHandler
{
    private readonly IRecetaRepository _repository;

    public GetRecetasHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RecetaResult>> Handle(Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        return _repository.GetAllAsync(hogarId, usuarioId, ct);
    }
}

public sealed class GetRecetasGuardadasHandler
{
    private readonly IRecetaRepository _repository;

    public GetRecetasGuardadasHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RecetaResult>> Handle(Guid hogarId, Guid usuarioId, CancellationToken ct)
        => _repository.GetSavedAsync(hogarId, usuarioId, ct);
}

public sealed class SaveRecetaHandler
{
    private readonly IRecetaRepository _repository;

    public SaveRecetaHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct)
        => _repository.SaveAsync(recetaId, hogarId, usuarioId, ct);
}

public sealed class UnsaveRecetaHandler
{
    private readonly IRecetaRepository _repository;

    public UnsaveRecetaHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(Guid recetaId, Guid hogarId, CancellationToken ct)
        => _repository.UnsaveAsync(recetaId, hogarId, ct);
}
