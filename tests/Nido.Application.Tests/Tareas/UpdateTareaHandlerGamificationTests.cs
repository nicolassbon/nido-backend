using Nido.Application.Tareas;

namespace Nido.Application.Tests.Tareas;

public sealed class UpdateTareaHandlerGamificationTests
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
        public TareaResult? UpdatedTarea { get; set; }
        public bool UpdateAsyncCalled { get; private set; }

        public Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(TareaToReturn);

        public Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion,
            DateTime? fechaLimite, string? estado, CancellationToken ct)
        {
            UpdateAsyncCalled = true;
            return Task.FromResult(UpdatedTarea);
        }

        public Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct) => Task.FromResult(new List<TareaResult>());
        public Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct) => Task.FromResult(MakeTask(Guid.NewGuid(), hogarId, "pendiente"));
        public Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct) => Task.FromResult<TareaResult?>(null);
        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct) => Task.FromResult(true);
        public Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct) => Task.FromResult(new List<DistribucionDiaResult>());
    }

    [Fact]
    public async Task Handle_PatchToCompletada_IsRejectedAndRepositoryNotCalled()
    {
        var taskId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        var handler = new UpdateTareaHandler(repo);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UpdateTareaCommand(taskId, hogarId, null, null, null, "completada"),
            CancellationToken.None));

        Assert.Contains("completar", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repo.UpdateAsyncCalled);
    }

    [Fact]
    public async Task Handle_PatchToCompletada_HasNoMaterializerDependency()
    {
        var taskId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        var handler = new UpdateTareaHandler(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UpdateTareaCommand(taskId, hogarId, null, null, null, "completada"),
            CancellationToken.None));

        Assert.False(repo.UpdateAsyncCalled);
    }

    [Fact]
    public async Task Handle_ReopenPath_LeavesUnlocksIntact_DoesNotMaterialize()
    {
        var completadoPor = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        var repo = new FakeTareaRepository
        {
            TareaToReturn = MakeTask(taskId, hogarId, "completada", completadoPor),
            UpdatedTarea = MakeTask(taskId, hogarId, "pendiente")
        };

        var handler = new UpdateTareaHandler(repo);

        var result = await handler.Handle(
            new UpdateTareaCommand(taskId, hogarId, null, null, null, "pendiente"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("pendiente", result!.Estado);
        Assert.True(repo.UpdateAsyncCalled);
    }
}
