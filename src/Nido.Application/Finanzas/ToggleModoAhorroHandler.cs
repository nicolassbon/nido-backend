using Nido.Application.Payments;

namespace Nido.Application.Finanzas;

public sealed class ToggleModoAhorroHandler
{
    private readonly IFinanzasRepository _repository;
    private readonly IEntitlementService _entitlementService;

    public ToggleModoAhorroHandler(
        IFinanzasRepository repository,
        IEntitlementService entitlementService)
    {
        _repository = repository;
        _entitlementService = entitlementService;
    }

    public async Task<bool?> GetModoAhorro(Guid hogarId, CancellationToken ct)
    {
        await _entitlementService.EnsurePremiumAsync(hogarId, ct);
        return await _repository.GetModoAhorroAsync(hogarId, ct);
    }

    public async Task<bool> Handle(ToggleModoAhorroCommand command, CancellationToken ct)
    {
        await _entitlementService.EnsurePremiumAsync(command.HogarId, ct);
        return await _repository.ToggleModoAhorroAsync(command.HogarId, command.Activo, ct);
    }
}
