using Nido.Application.Tareas;

namespace Nido.Application.Tests.Tareas;

public sealed class CompletarTareaHandlerGamificationTests
{
    private static TareaResult MakeTask(Guid id, Guid hogarId, string estado, Guid? completadoPor = null)
        => new(
            Id: id,
            HogarId: hogarId,
            Titulo: "Test Task",
            Descripcion: null,
            Estado: estado,
            FechaLimite: null,
            FechaCompletado: completadoPor.HasValue ? DateTime.UtcNow : null,
            CreadoPor: Guid.NewGuid(),
            CreadoPorNombre: "Creator",
            CompletadoPor: completadoPor,
            CompletadoPorNombre: completadoPor.HasValue ? "Completer" : null,
            AsignadoA: null,
            CreatedAt: DateTime.UtcNow);

    private sealed class FakeTareaRepository : ITareaRepository
    {
        public TareaResult? TareaToReturn { get; set; }
        public TareaResult? TareaCompletada { get; set; }

        public Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct)
            => Task.FromResult(TareaCompletada);

        public Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(TareaToReturn);

        // Unused stub methods
        public Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct) => Task.FromResult(MakeTask(Guid.NewGuid(), hogarId, "pendiente"));
        public Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion, DateTime? fechaLimite, string? estado, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct) => Task.FromResult(true);
        public Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct) => Task.FromResult(new List<DistribucionDiaResult>());
    }

    [Fact]
    public async Task Handle_SuccessfulCompletion_TriggersMaterializationForCompletingUser()
    {
        var completadoPor = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var repo = new FakeTareaRepository
        {
            TareaToReturn = MakeTask(taskId, hogarId, "pendiente"),
            TareaCompletada = MakeTask(taskId, hogarId, "completada", completadoPor)
        };

        var materializer = new FakeGamificationUnlockMaterializer();
        var handler = new CompletarTareaHandler(repo, materializer);

        await handler.Handle(new CompletarTareaCommand(taskId, hogarId, completadoPor), CancellationToken.None);

        Assert.Single(materializer.MaterializationCalls);
        Assert.Equal(completadoPor, materializer.MaterializationCalls[0]);
    }

    [Fact]
    public async Task Handle_AlreadyCompletedTask_DoesNotRematerializeUnlocks()
    {
        var completadoPor = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var repo = new FakeTareaRepository
        {
            TareaToReturn = MakeTask(taskId, hogarId, "completada", completadoPor),
            TareaCompletada = MakeTask(taskId, hogarId, "completada", completadoPor)
        };

        var materializer = new FakeGamificationUnlockMaterializer();
        var handler = new CompletarTareaHandler(repo, materializer);

        await handler.Handle(new CompletarTareaCommand(taskId, hogarId, completadoPor), CancellationToken.None);

        Assert.Empty(materializer.MaterializationCalls);
    }

    [Fact]
    public async Task Handle_TaskNotFound_DoesNotMaterialize()
    {
        var repo = new FakeTareaRepository(); // TaskToReturn is null by default
        var materializer = new FakeGamificationUnlockMaterializer();
        var handler = new CompletarTareaHandler(repo, materializer);

        var result = await handler.Handle(
            new CompletarTareaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(materializer.MaterializationCalls);
    }
}
