using Nido.Domain.Productos;
using Nido.Domain.StockHogar;

namespace Nido.Application.Productos;

public sealed class CreateProductoHandler
{
    private readonly IProductRepository _productoRepository;
    private readonly IStockHogarRepository _stockHogarRepository;

    public CreateProductoHandler(
        IProductRepository productoRepository,
        IStockHogarRepository stockHogarRepository)
    {
        _productoRepository = productoRepository;
        _stockHogarRepository = stockHogarRepository;
    }

    public async Task<CreateProductoResult> Handle(
        CreateProductoCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre es requerido.");
        }

        if (command.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        }

        var producto = new Producto(
            command.Nombre,
            command.CategoriaId,
            null,
            null
        );

        await _productoRepository.SaveAsync(
            producto,
            cancellationToken);

        var stockHogar = new StockHogar(
            command.HogarId,
            producto.Id,
            command.Cantidad,
            command.UnidadMedida,
            command.FechaVencimiento,
            command.UsuarioIngresoId
        );

        await _stockHogarRepository.SaveAsync(
            stockHogar,
            cancellationToken);

        return new CreateProductoResult(
            producto.Id,
            stockHogar.Id,
            producto.Nombre,
            stockHogar.Cantidad,
            stockHogar.UnidadMedida
        );
    }
}