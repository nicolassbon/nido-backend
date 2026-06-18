using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class UpdateGastoHandler
{
    private readonly IFinanzasRepository _repo;

    public UpdateGastoHandler(IFinanzasRepository repo) => _repo = repo;

    public async Task<GastoResult> Handle(UpdateGastoCommand command, CancellationToken ct)
    {
        var result = await _repo.UpdateGastoAsync(command, ct)
            ?? throw new GastoNotFoundException();

        return result;
    }
}
