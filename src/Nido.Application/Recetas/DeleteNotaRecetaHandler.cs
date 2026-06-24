namespace Nido.Application.Recetas;

public sealed record DeleteNotaRecetaCommand(
    Guid NotaId,
    Guid HogarId,
    Guid UsuarioId);

public sealed class DeleteNotaRecetaHandler
{
    private readonly INotaRecetaRepository _repository;

    public DeleteNotaRecetaHandler(INotaRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(DeleteNotaRecetaCommand command, CancellationToken ct)
        => _repository.DeleteAsync(command.NotaId, command.HogarId, command.UsuarioId, ct);
}
