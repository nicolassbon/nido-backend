namespace Nido.Application.Recetas;

public sealed class GetRecetaByIdHandler
{
    private readonly IRecetaRepository _repository;

    public GetRecetaByIdHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetRecetaByIdResult?> Handle(
        GetRecetaByIdCommand command,
        CancellationToken ct)
    {
        if (command.Id == Guid.Empty || command.HogarId == Guid.Empty)
        {
            return null;
        }

        var result = await _repository.GetByIdAsync(command.Id, command.HogarId, ct);
        
        if (result is null)
        {
            return null;
        }

        return new GetRecetaByIdResult(
            result.Id,
            result.Nombre,
            result.Descripcion,
            result.TiempoCoccionMin,
            result.Dificultad,
            result.Porciones,
            result.FuenteId,
            result.ImagenUrl,
            result.Calorias,
            result.Proteinas,
            result.Carbohidratos,
            result.Grasas,
            result.Ingredientes,
            result.Pasos,
            result.Electrodomesticos,
            result.VecesCocinada);
    }
}
