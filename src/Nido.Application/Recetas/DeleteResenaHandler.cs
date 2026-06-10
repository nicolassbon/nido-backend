namespace Nido.Application.Recetas;

public sealed record DeleteResenaCommand(Guid RecetaId, Guid UsuarioId);

public sealed class DeleteResenaHandler
{
    private readonly IResenaRecetaRepository _repository;

    public DeleteResenaHandler(IResenaRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteResenaCommand command, CancellationToken ct)
    {
        await _repository.DeleteAsync(command.RecetaId, command.UsuarioId, ct);
    }
}
