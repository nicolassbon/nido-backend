
using Nido.Domain.StockHogar;
using Nido.Application.Productos.Exceptions;

namespace Nido.Application.Productos;

public sealed class CreateStockHomeHandler
{
    private readonly IStockHogarRepository _stockHogarRepository;
    private readonly IProductoRepository _productoRepository;

    public CreateStockHomeHandler(
        IStockHogarRepository stockHogarRepository,
        IProductoRepository productoRepository)
    {
        _stockHogarRepository = stockHogarRepository;
        _productoRepository = productoRepository;
    }

    public async Task<CreateStockHomeResult> Handle(
        CreateStockHomeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new MissingProductFieldException("nombre");
        }

        if (command.CantidadActual  <= 0)
        {
            throw new MissingProductFieldException("cantidad");
        }

        if (string.IsNullOrWhiteSpace(command.Ubicacion))
        {
            throw new MissingProductFieldException("ubicacion");
        }

        var producto = await _productoRepository.GetByNameAsync(
            command.Nombre,
            cancellationToken);

        if (producto is null)
        {
            throw new ArgumentException($"No existe un producto precargado con nombre '{command.Nombre}'.");
        }

        var cantidadEnvases = command.CantidadEnvases < 1 ? 1 : command.CantidadEnvases;

        var stockHogar = new StockHogar(
            command.HogarId,
            producto.Id,
            command.CantidadActual,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.UsuarioIngresoId,
            command.Ubicacion,
            false,
            0,
            cantidadEnvases
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
            producto.CategoriaId ?? command.CategoriaId,
            stockHogar.CantidadEnvases
        );
    }
}
