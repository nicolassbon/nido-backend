using Nido.Application.Alacena.Exceptions;
using System.Globalization;

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
        if (!string.IsNullOrWhiteSpace(command.FechaVencimiento)
            && !DateOnly.TryParseExact(command.FechaVencimiento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
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
            command.PorcentajeConsumido);

        return await _repository.CreateAsync(request, ct);
    }
}
