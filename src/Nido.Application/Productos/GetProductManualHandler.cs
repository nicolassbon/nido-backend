namespace Nido.Application.Productos;

public sealed class GetProductManualHandler
{
    private readonly IProductManualRepository _repository;

    public GetProductManualHandler(IProductManualRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<GetProductManualResult>> Handle(
        GetProductManualCommand command,
        CancellationToken cancellationToken)
    {
        if (command.HogarId == Guid.Empty)
        {
            throw new ArgumentException("El hogar es requerido.");
        }

        return await _repository.GetManualByHogarAsync(
            command.HogarId,
            cancellationToken);
    }
}