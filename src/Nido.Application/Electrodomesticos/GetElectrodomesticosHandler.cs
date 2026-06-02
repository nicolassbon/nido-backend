using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Electrodomesticos;

public sealed class GetElectrodomesticosHandler
{
    private readonly IElectrodomesticoRepository _repository;

    public GetElectrodomesticosHandler(IElectrodomesticoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CreateElectrodomesticoResult>> Handle(CancellationToken cancellationToken)
    {
        var electrodomesticos = await _repository.GetAllAsync(cancellationToken);

        return electrodomesticos
            .Select(electrodomestico => new CreateElectrodomesticoResult(
                electrodomestico.Id,
                electrodomestico.HogarId,
                electrodomestico.Nombre,
                electrodomestico.Tipo,
                electrodomestico.Estado,
                electrodomestico.Marca,
                electrodomestico.ImagenUrl,
                electrodomestico.CatalogoId
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<CreateElectrodomesticoResult>> HandleByHogar(Guid hogarId, CancellationToken cancellationToken)
    {
        var electrodomesticos = await _repository.GetByHogarIdAsync(hogarId, cancellationToken);

        return electrodomesticos
            .Select(electrodomestico => new CreateElectrodomesticoResult(
                electrodomestico.Id,
                electrodomestico.HogarId,
                electrodomestico.Nombre,
                electrodomestico.Tipo,
                electrodomestico.Estado,
                electrodomestico.Marca,
                electrodomestico.ImagenUrl,
                electrodomestico.CatalogoId
            ))
            .ToList();
    }
}
