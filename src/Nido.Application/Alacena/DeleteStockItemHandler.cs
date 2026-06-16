using System.Globalization;
using Nido.Application.Insights;

namespace Nido.Application.Alacena;

public sealed class DeleteStockItemHandler
{
    private readonly IAlacenaRepository _repository;
    private readonly IConsumoProductoRepository _consumos;

    public DeleteStockItemHandler(
        IAlacenaRepository repository,
        IConsumoProductoRepository consumos)
    {
        _repository = repository;
        _consumos = consumos;
    }

    public async Task<bool> Handle(DeleteStockItemCommand command, CancellationToken ct)
    {
        var item = command.HogarId != Guid.Empty
            ? await _repository.GetByIdAsync(command.Id, command.HogarId, ct)
            : null;

        var deleted = await _repository.DeleteAsync(command.Id, command.HogarId, ct);
        if (!deleted) return false;

        if (item is not null)
        {
            var motivo = EsVencido(item.FechaVencimiento)
                ? ConsumoMotivos.Vencido
                : ConsumoMotivos.Terminado;

            await _consumos.RegistrarAsync(new RegistrarConsumoInput(
                command.HogarId,
                item.ProductoId,
                item.Nombre,
                null,
                item.Cantidad,
                item.UnidadMedida,
                motivo,
                command.UsuarioId == Guid.Empty ? null : command.UsuarioId), ct);
        }

        return true;
    }

    private static bool EsVencido(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha)) return false;
        if (!DateOnly.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return false;
        return d < DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
