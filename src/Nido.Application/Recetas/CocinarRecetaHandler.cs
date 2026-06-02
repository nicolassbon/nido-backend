namespace Nido.Application.Recetas;

public sealed class CocinarRecetaHandler
{
    private readonly IRecetaRepository _repository;

    public CocinarRecetaHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<CocinarRecetaResult?> Handle(CocinarRecetaCommand command, CancellationToken ct)
    {
        if (command.RecetaId == Guid.Empty || command.HogarId == Guid.Empty)
            return null;

        return await _repository.CocinarAsync(command, ct);
    }
}
