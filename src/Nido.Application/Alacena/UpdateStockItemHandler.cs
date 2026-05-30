namespace Nido.Application.Alacena;

public sealed class UpdateStockItemHandler
{
    private readonly IAlacenaRepository _repository;

    public UpdateStockItemHandler(IAlacenaRepository repository)
    {
        _repository = repository;
    }

    public async Task<StockItemResult?> Handle(UpdateStockItemCommand command, CancellationToken ct)
    {
        if (command.FechaVencimiento is not null && command.FechaVencimiento.Length > 0 && !DateOnly.TryParse(command.FechaVencimiento, out _))
        {
            throw new ArgumentException("La fecha de vencimiento debe tener formato yyyy-MM-dd.");
        }

        var request = new UpdateStockItemRequestModel(
            command.Id,
            command.UsuarioId,
            command.Cantidad,
            command.Ubicacion,
            command.FechaVencimiento,
            command.EstaAbierto,
            command.PorcentajeConsumido);

        return await _repository.UpdateAsync(request, ct);
    }
}
