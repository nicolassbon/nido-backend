using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class GetBalanceHandler
{
    private readonly IFinanzasRepository _repository;

    public GetBalanceHandler(IFinanzasRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetBalanceResult> Handle(GetBalanceQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Desde) && !DateOnly.TryParse(query.Desde, out _))
            throw new InvalidGastoFechaException();

        if (!string.IsNullOrWhiteSpace(query.Hasta) && !DateOnly.TryParse(query.Hasta, out _))
            throw new InvalidGastoFechaException();

        return await _repository.GetBalanceAsync(query, ct);
    }
}
