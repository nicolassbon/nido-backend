namespace Nido.Application.Alacena;

public sealed class DeleteStockItemHandler
{
    private readonly IAlacenaRepository _repository;

    public DeleteStockItemHandler(IAlacenaRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteStockItemCommand command, CancellationToken ct)
    {
        return await _repository.DeleteAsync(command.Id, ct);
    }
}
