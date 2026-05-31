
using Nido.Domain.StockHogar;

namespace Nido.Application.Productos;

public sealed class CreateStockHomeHandler
{
    private readonly IStockHogarRepository _stockHogarRepository;

    public CreateStockHomeHandler(IStockHogarRepository stockHogarRepository)
    {
        _stockHogarRepository = stockHogarRepository;
    }

    public async Task<CreateStockHomeResult> Handle(
        CreateStockHomeCommand command,
        CancellationToken cancellationToken)
    {

        if (command.CantidadActual  <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        }


        var stockHogar = new StockHogar(
            command.HogarId,
            command.ProductoId,
            command.CantidadActual,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.UsuarioIngresoId,
            command.Ubicaciom,
            command.estaAbierto,
            command.porcentajeConsumido
        );

        await _stockHogarRepository.SaveAsync(
            stockHogar,
            cancellationToken);

        return new CreateStockHomeResult(
            stockHogar.Id,
            stockHogar.ProductoId,
            stockHogar.CantidadActual,
            stockHogar.UnidadMedida,
            stockHogar.FechaVencimiento,
            stockHogar.UsuarioIngresoId,
            stockHogar.Ubicacion,
            stockHogar.EstaAbierto,
            stockHogar.PorcentajeConsumido,
            command.CategoriaId
          
        );
    }
}