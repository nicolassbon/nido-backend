using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Electrodomesticos;

public sealed record DeleteElectrodomesticoCommand(Guid Id, Guid HogarId);

public sealed class DeleteElectrodomesticoHandler
{
    private readonly IElectrodomesticoRepository _repository;

    public DeleteElectrodomesticoHandler(IElectrodomesticoRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(DeleteElectrodomesticoCommand command, CancellationToken cancellationToken) =>
        _repository.DeleteAsync(command.Id, command.HogarId, cancellationToken);
}
