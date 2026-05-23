using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Electrodomesticos;

public sealed class CreateElectrodomesticoHandler
{
    private readonly IElectrodomesticoRepository _repository;

    public CreateElectrodomesticoHandler(IElectrodomesticoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateElectrodomesticoResult> Handle(
        CreateElectrodomesticoCommand command,
        CancellationToken cancellationToken)
    {
        if (command.HogarId == Guid.Empty)
        {
            throw new ArgumentException("El hogar es requerido.");
        }

        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre es requerido.");
        }

        var hogarExiste = await _repository.HogarExisteAsync(command.HogarId, cancellationToken);

        if (!hogarExiste)
        {
            throw new InvalidOperationException("El hogar indicado no existe.");
        }

        var electrodomestico = new Electrodomestico(
            command.HogarId,
            command.Nombre,
            command.Tipo,
            command.Estado
        );

        await _repository.SaveAsync(electrodomestico, cancellationToken);

        return new CreateElectrodomesticoResult(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado
        );
    }
}