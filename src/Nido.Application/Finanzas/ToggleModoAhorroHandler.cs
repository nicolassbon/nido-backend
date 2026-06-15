namespace Nido.Application.Finanzas;

public sealed class ToggleModoAhorroHandler
{
    private readonly IFinanzasRepository _repository;

    public ToggleModoAhorroHandler(IFinanzasRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool?> GetModoAhorro(Guid hogarId, CancellationToken ct)
        => await _repository.GetModoAhorroAsync(hogarId, ct);

    public async Task<bool> Handle(ToggleModoAhorroCommand command, CancellationToken ct)
        => await _repository.ToggleModoAhorroAsync(command.HogarId, command.Activo, ct);
}
