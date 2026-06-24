using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class DeleteGastoHandler
{
    private readonly IFinanzasRepository _repo;

    public DeleteGastoHandler(IFinanzasRepository repo) => _repo = repo;

    public async Task<DeleteGastoResult> Handle(Guid gastoId, Guid hogarId, CancellationToken ct)
    {
        var result = await _repo.DeleteGastoAsync(gastoId, hogarId, ct)
            ?? throw new GastoNotFoundException();

        return result;
    }
}
