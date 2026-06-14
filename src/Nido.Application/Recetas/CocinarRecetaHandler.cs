using Nido.Application.Alacena;
using Nido.Application.Insights;

namespace Nido.Application.Recetas;

public sealed class CocinarRecetaHandler
{
    private readonly IRecetaRepository _repository;
    private readonly IAlacenaRepository _alacena;
    private readonly IConsumoProductoRepository _consumos;

    public CocinarRecetaHandler(
        IRecetaRepository repository,
        IAlacenaRepository alacena,
        IConsumoProductoRepository consumos)
    {
        _repository = repository;
        _alacena = alacena;
        _consumos = consumos;
    }

    public async Task<CocinarRecetaResult?> Handle(CocinarRecetaCommand command, CancellationToken ct)
    {
        if (command.RecetaId == Guid.Empty || command.HogarId == Guid.Empty)
            return null;

        // Snapshot del stock antes para detectar qué se consumió al cocinar.
        var antes = await _alacena.GetByHogarAsync(command.HogarId, ct);

        var result = await _repository.CocinarAsync(command, ct);
        if (result is null) return null;

        var despues = await _alacena.GetByHogarAsync(command.HogarId, ct);

        await RegistrarConsumosAsync(command, antes, despues, ct);

        return result;
    }

    private async Task RegistrarConsumosAsync(
        CocinarRecetaCommand command,
        IReadOnlyList<StockItemResult> antes,
        IReadOnlyList<StockItemResult> despues,
        CancellationToken ct)
    {
        var indiceDespues = despues.ToDictionary(s => s.Id, s => s);

        foreach (var prev in antes)
        {
            if (!indiceDespues.TryGetValue(prev.Id, out var post))
            {
                if (prev.Cantidad > 0)
                {
                    await _consumos.RegistrarAsync(new RegistrarConsumoInput(
                        command.HogarId,
                        prev.ProductoId,
                        prev.Nombre,
                        null,
                        prev.Cantidad,
                        prev.UnidadMedida,
                        ConsumoMotivos.Cocinado,
                        command.UsuarioId), ct);
                }
                continue;
            }

            if (post.Cantidad < prev.Cantidad)
            {
                await _consumos.RegistrarAsync(new RegistrarConsumoInput(
                    command.HogarId,
                    prev.ProductoId,
                    prev.Nombre,
                    null,
                    prev.Cantidad - post.Cantidad,
                    prev.UnidadMedida,
                    ConsumoMotivos.Cocinado,
                    command.UsuarioId), ct);
            }
        }
    }
}
