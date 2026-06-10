using Nido.Application.Alacena.Exceptions;

namespace Nido.Application.Alacena;

public sealed class CreateStockItemHandler
{
    private readonly IAlacenaRepository _repository;

    public CreateStockItemHandler(IAlacenaRepository repository)
    {
        _repository = repository;
    }

    public async Task<StockItemResult> Handle(CreateStockItemCommand command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.FechaVencimiento) && !DateOnly.TryParse(command.FechaVencimiento, out _))
        {
            throw new InvalidStockItemDateException();
        }

        var request = new CreateStockItemRequestModel(
            command.HogarId,
            command.UsuarioId,
            command.Nombre,
            command.CodigoBarras,
            command.Imagen,
            command.Ubicacion,
            command.Cantidad,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.EstaAbierto,
            command.PorcentajeConsumido,
            command.CantidadEnvases < 1 ? 1 : command.CantidadEnvases);

        return await _repository.CreateAsync(request, ct);
    }
}
