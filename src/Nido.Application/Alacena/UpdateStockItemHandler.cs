using Nido.Application.Alacena.Exceptions;

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
            throw new InvalidStockItemDateException();
        }

        var request = new UpdateStockItemRequestModel(
            command.Id,
            command.UsuarioId,
            command.Nombre,
            command.Cantidad,
            command.Ubicacion,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.EstaAbierto,
            command.PorcentajeConsumido,
            command.CantidadEnvases);

        return await _repository.UpdateAsync(request, ct);
    }
}
