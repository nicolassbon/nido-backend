namespace Nido.Domain.Productos;

public interface IProductRepository
{
    Task SaveAsync(
        Producto producto,
        CancellationToken cancellationToken);


    
}