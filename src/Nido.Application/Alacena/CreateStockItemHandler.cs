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

        var origenCarga = StockLoadOrigins.Normalize(command.OrigenCarga, command.CodigoBarras);

        var request = new CreateStockItemRequestModel(
            command.HogarId,
            command.UsuarioId,
            command.Nombre,
            command.CategoriaId,
            command.CodigoBarras,
            command.Imagen,
            command.Ubicacion,
            command.Cantidad,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.EstaAbierto,
            command.PorcentajeConsumido,
            command.CantidadEnvases < 1 ? 1 : command.CantidadEnvases,
            origenCarga,
            command.Calorias,
            command.Proteinas,
            command.Carbohidratos,
            command.Grasas);

        return await _repository.CreateAsync(request, ct);
    }
}
