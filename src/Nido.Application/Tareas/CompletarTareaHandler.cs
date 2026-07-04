using Nido.Application.Gamificacion;

namespace Nido.Application.Tareas;

public sealed record CompletarTareaCommand(Guid Id, Guid HogarId, Guid CompletadoPor);

public sealed class CompletarTareaHandler
{
    private readonly ITareaRepository _repository;
    private readonly IGamificationUnlockMaterializer _materializer;

    public CompletarTareaHandler(ITareaRepository repository, IGamificationUnlockMaterializer materializer)
    {
        _repository = repository;
        _materializer = materializer;
    }

    public async Task<TareaResult?> Handle(CompletarTareaCommand command, CancellationToken ct)
    {
        // Load current task to check if already completed
        var existing = await _repository.GetByIdAsync(command.Id, command.HogarId, ct);
        if (existing is null) return null;

        // Idempotent: if already completed, return unchanged, no unlock write
        if (existing.Estado == "completada")
            return existing;

        var result = await _repository.CompletarAsync(command.Id, command.HogarId, command.CompletadoPor, ct);
        if (result is null) return null;

        // Trigger unlock materialization for the completing user
        await _materializer.MaterializeEligibleUnlocksAsync(command.CompletadoPor, ct);

        return result;
    }
}
