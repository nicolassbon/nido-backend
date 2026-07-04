using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Electrodomesticos;

public sealed class UpdateElectrodomesticoHandler
{
    private readonly IElectrodomesticoRepository _repository;

    public UpdateElectrodomesticoHandler(IElectrodomesticoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateElectrodomesticoResult?> Handle(
        UpdateElectrodomesticoCommand command,
        CancellationToken cancellationToken)
    {
        var electrodomestico = await _repository.UpdateAsync(
            command.Id, command.HogarId, command.Tipo, command.Estado, cancellationToken);

        if (electrodomestico is null) return null;

        return new CreateElectrodomesticoResult(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado,
            electrodomestico.Marca,
            electrodomestico.ImagenUrl,
            electrodomestico.CatalogoId
        );
    }
}
