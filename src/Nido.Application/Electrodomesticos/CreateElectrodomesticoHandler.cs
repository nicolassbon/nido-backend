using Nido.Application.Electrodomesticos.Exceptions;
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
            throw new MissingApplianceFieldException("hogar");
        }

        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new MissingApplianceFieldException("nombre");
        }

        var hogarExiste = await _repository.HogarExisteAsync(command.HogarId, cancellationToken);

        if (!hogarExiste)
        {
            throw new HouseholdNotFoundException();
        }

        var electrodomestico = new Electrodomestico(
            command.HogarId,
            command.Nombre,
            command.Tipo,
            command.Estado,
            command.Marca,
            command.ImagenUrl
        );

        await _repository.SaveAsync(electrodomestico, cancellationToken);

        return new CreateElectrodomesticoResult(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado,
            electrodomestico.Marca,
            electrodomestico.ImagenUrl
        );
    }
}
