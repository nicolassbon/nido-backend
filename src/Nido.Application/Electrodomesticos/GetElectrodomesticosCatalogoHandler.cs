using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Electrodomesticos;

public sealed class GetElectrodomesticosCatalogoHandler
{
    private readonly IElectrodomesticoRepository _repository;

    public GetElectrodomesticosCatalogoHandler(IElectrodomesticoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ElectrodomesticoCatalogoResult>> Handle(
        CancellationToken cancellationToken)
    {
        var catalogo = await _repository.GetCatalogoAsync(cancellationToken);

        return catalogo
            .Where(item => item.Activo)
            .OrderBy(item => item.Orden)
            .Select(item => new ElectrodomesticoCatalogoResult(
                item.Id,
                item.Nombre,
                item.Tipo,
                item.Icono,
                item.ImagenUrl,
                item.Orden))
            .ToList();
    }
}