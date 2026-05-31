namespace Nido.Application.Productos;

public interface IProductManualRepository
{
    Task<IReadOnlyList<GetProductManualResult>> GetManualByHogarAsync(
        Guid hogarId,
        CancellationToken cancellationToken);
}