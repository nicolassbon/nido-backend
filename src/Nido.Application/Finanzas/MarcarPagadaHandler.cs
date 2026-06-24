using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class MarcarPagadaHandler
{
    private readonly IFinanzasRepository _repository;

    public MarcarPagadaHandler(IFinanzasRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagarFacturaResult> Handle(
        Guid facturaId, Guid hogarId, Guid pagadoPorId, CancellationToken ct)
    {
        var factura = await _repository.MarcarPagadaAsync(facturaId, hogarId, ct)
            ?? throw new FacturaNotFoundException();

        GastoResult? gastoCreado = null;
        if (factura.Monto.HasValue)
        {
            var command = new CreateGastoCommand(
                HogarId:     hogarId,
                PagadoPorId: pagadoPorId,
                Monto:       factura.Monto.Value,
                Descripcion: factura.Nombre,
                Categoria:   "Servicios",
                Fecha:       DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                FacturaId:   facturaId);

            gastoCreado = await _repository.CreateGastoAsync(command, ct);
        }

        return new PagarFacturaResult(factura, gastoCreado);
    }
}
