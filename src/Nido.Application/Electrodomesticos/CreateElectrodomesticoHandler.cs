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

        if (!command.CatalogoId.HasValue && string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new MissingApplianceFieldException("nombre");
        }

        var hogarExiste = await _repository.HogarExisteAsync(command.HogarId, cancellationToken);

        if (!hogarExiste)
        {
            throw new HouseholdNotFoundException();
        }

        ElectrodomesticoCatalogo? catalogoItem = null;

        if (command.CatalogoId.HasValue)
        {
            var catalogo = await _repository.GetCatalogoAsync(cancellationToken);
            catalogoItem = catalogo.FirstOrDefault(item => item.Id == command.CatalogoId.Value);

            if (catalogoItem is null)
            {
                throw new ApplianceCatalogItemNotFoundException();
            }
        }

        var nombre = catalogoItem?.Nombre ?? command.Nombre!.Trim();
        var tipo = catalogoItem?.Tipo ?? command.Tipo;
        var imagenUrl = catalogoItem?.ImagenUrl;

        var electrodomestico = new Electrodomestico(
            command.HogarId,
            catalogoItem?.Id,
            nombre,
            tipo,
            command.Estado,
            command.Marca,
            imagenUrl
        );

        await _repository.SaveAsync(electrodomestico, cancellationToken);

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
