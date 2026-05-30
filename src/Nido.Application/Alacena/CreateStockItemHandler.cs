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
            throw new ArgumentException("La fecha de vencimiento debe tener formato yyyy-MM-dd.");
        }

        var request = new CreateStockItemRequestModel(
            command.HogarId,
            command.UsuarioId,
            command.Nombre,
            command.CodigoBarras,
            command.Imagen,
            command.Ubicacion,
            command.Cantidad,
            command.FechaVencimiento,
            command.EstaAbierto,
            command.PorcentajeConsumido);

        return await _repository.CreateAsync(request, ct);
    }
}
