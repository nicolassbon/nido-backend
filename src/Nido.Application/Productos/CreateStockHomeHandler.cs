using System.Globalization;
using Nido.Application.Productos.Exceptions;
using Nido.Domain.StockHogar;

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

        if (command.CantidadActual <= 0)
        {
            throw new MissingProductFieldException("cantidad");
        }

        if (string.IsNullOrWhiteSpace(command.Ubicacion))
        {
            throw new MissingProductFieldException("ubicacion");
        }

        DateOnly? fechaVencimiento = null;
        if (!string.IsNullOrWhiteSpace(command.FechaVencimiento))
        {
            if (!DateOnly.TryParseExact(
                    command.FechaVencimiento,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedFechaVencimiento))
            {
                throw new ArgumentException("La fecha de vencimiento debe tener formato yyyy-MM-dd.");
            }

            fechaVencimiento = parsedFechaVencimiento;
        }

        var producto = await _productoRepository.CreateAsync(
            command.Nombre,
            command.CategoriaId is null || command.CategoriaId == Guid.Empty
                ? null
                : command.CategoriaId,
            cancellationToken,
            command.Calorias,
            command.Proteinas,
            command.Carbohidratos,
            command.Grasas);

        var cantidadEnvases = command.CantidadEnvases < 1 ? 1 : command.CantidadEnvases;

        var stockHogar = new StockHogar(
            command.HogarId,
            producto.Id,
            command.CantidadActual,
            command.UnidadMedida,
            fechaVencimiento?.ToDateTime(TimeOnly.MinValue),
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
            fechaVencimiento?.ToString("yyyy-MM-dd"),
            stockHogar.UsuarioIngresoId,
            stockHogar.Ubicacion,
            stockHogar.EstaAbierto,
            stockHogar.PorcentajeConsumido,
            producto.CategoriaId,
            stockHogar.CantidadEnvases
        );
    }
}
