using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class GetFacturasHandler
{
    private readonly IFinanzasRepository _repository;

    public GetFacturasHandler(IFinanzasRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FacturaResult>> Handle(GetFacturasQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            var tiposValidos = new HashSet<string> { "servicio", "suscripcion" };
            if (!tiposValidos.Contains(query.Tipo))
                throw new InvalidFacturaTipoException(query.Tipo);
        }

        return await _repository.GetFacturasAsync(query, ct);
    }
}
