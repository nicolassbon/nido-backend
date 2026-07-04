using Nido.Application.Gamificacion;
using Nido.Application.Common.Security;
using Nido.Application.Tareas;

namespace Nido.Application.Tests.Tareas;

public sealed class TareasHandlersTests
{
    [Fact]
    public async Task GetTareas_WhenTareasExist_ReturnsList()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.TareasDeHogar = [MakeTarea(hogarId, "Limpiar cocina"), MakeTarea(hogarId, "Sacar basura")];
        var handler = new GetTareasHandler(repo);

        var result = await handler.Handle(new GetTareasQuery(hogarId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Titulo == "Limpiar cocina");
        Assert.Contains(result, t => t.Titulo == "Sacar basura");
    }

    [Fact]
    public async Task GetTareas_WhenNoTareas_ReturnsEmptyList()
    {
        var handler = new GetTareasHandler(new FakeTareaRepository());

        var result = await handler.Handle(new GetTareasQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMisTareas_PassesHogarIdAndUsuarioIdToRepo()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.TareasAsignadas = [MakeTarea(hogarId, "Mi tarea")];
        var handler = new GetMisTareasHandler(repo);

        var result = await handler.Handle(new GetMisTareasQuery(hogarId, usuarioId), CancellationToken.None);

        Assert.Equal(hogarId, repo.LastGetByAsignadoHogarId);
        Assert.Equal(usuarioId, repo.LastGetByAsignadoUsuarioId);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetMisTareas_WhenNoAssignedTareas_ReturnsEmptyList()
    {
        var handler = new GetMisTareasHandler(new FakeTareaRepository());

        var result = await handler.Handle(
            new GetMisTareasQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateTarea_PassesAllCommandFieldsToRepo()
    {
        var hogarId = Guid.NewGuid();
        var creadoPor = Guid.NewGuid();
        var asignadoA = Guid.NewGuid();
        var fechaLimite = DateTime.UtcNow.AddDays(7);
        var repo = new FakeTareaRepository();
        repo.TareaCreada = MakeTarea(hogarId, "Regar plantas");
        var handler = new CreateTareaHandler(repo, new AllowAllMembershipService());

        var result = await handler.Handle(
            new CreateTareaCommand(hogarId, creadoPor, "Regar plantas", "Todas las macetas", fechaLimite, asignadoA),
            CancellationToken.None);

        Assert.Equal(hogarId, repo.LastCreateHogarId);
        Assert.Equal(creadoPor, repo.LastCreateCreadoPor);
        Assert.Equal("Regar plantas", repo.LastCreateTitulo);
        Assert.Equal("Todas las macetas", repo.LastCreateDescripcion);
        Assert.Equal(fechaLimite, repo.LastCreateFechaLimite);
        Assert.Equal(asignadoA, repo.LastCreateAsignadoA);
        Assert.Equal("Regar plantas", result.Titulo);
    }

    [Fact]
    public async Task CreateTarea_WithNullDescripcionAndNoAssignee_DoesNotThrow()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.TareaCreada = MakeTarea(hogarId, "Orden general");
        var handler = new CreateTareaHandler(repo, new AllowAllMembershipService());

        var result = await handler.Handle(
            new CreateTareaCommand(hogarId, Guid.NewGuid(), "Orden general", null, null, null),
            CancellationToken.None);

        Assert.Null(repo.LastCreateDescripcion);
        Assert.Null(repo.LastCreateAsignadoA);
        Assert.Equal("Orden general", result.Titulo);
    }

    [Fact]
    public async Task CreateTarea_WhenAsignadoAIsNull_DoesNotInvokeMembershipService()
    {
        var repo = new FakeTareaRepository();
        repo.TareaCreada = MakeTarea(Guid.NewGuid(), "Sin asignar");
        var membership = new AllowAllMembershipService();
        var handler = new CreateTareaHandler(repo, membership);

        await handler.Handle(
            new CreateTareaCommand(Guid.NewGuid(), Guid.NewGuid(), "Sin asignar", null, null, null),
            CancellationToken.None);

        Assert.Equal(0, membership.EnsureMemberCallCount);
        Assert.Equal(1, repo.CreateCallCount);
    }

    [Fact]
    public async Task CreateTarea_WhenAsignadoANotMemberOfHousehold_ThrowsAndDoesNotCallRepository()
    {
        var repo = new FakeTareaRepository();
        var handler = new CreateTareaHandler(repo, new DenyAllMembershipService());
        var hogarId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateTareaCommand(hogarId, Guid.NewGuid(), "Fuga asignada", null, null, Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(0, repo.CreateCallCount);
    }

    [Fact]
    public async Task CreateTarea_WhenAsignadoAIsMemberOfHousehold_CreatesTarea()
    {
        var hogarId = Guid.NewGuid();
        var asignadoA = Guid.NewGuid();
        var membership = new SelectiveMembershipService(allow: asignadoA);
        var repo = new FakeTareaRepository();
        repo.TareaCreada = MakeTarea(hogarId, "Asignada válida");
        var handler = new CreateTareaHandler(repo, membership);

        var result = await handler.Handle(
            new CreateTareaCommand(hogarId, Guid.NewGuid(), "Asignada válida", null, null, asignadoA),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, membership.EnsureMemberCallCount);
        Assert.Equal(asignadoA, repo.LastCreateAsignadoA);
        Assert.Equal(1, repo.CreateCallCount);
    }

    [Fact]
    public async Task CompletarTarea_WhenTareaExists_ReturnsUpdatedTarea()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.TareaCompletada = MakeTarea(hogarId, "Limpiar", estado: "completada");
        repo.TareaToReturn = MakeTarea(hogarId, "Limpiar");
        var handler = new CompletarTareaHandler(repo, new FakeGamificationUnlockMaterializer());

        var result = await handler.Handle(
            new CompletarTareaCommand(Guid.NewGuid(), hogarId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("completada", result!.Estado);
    }

    [Fact]
    public async Task CompletarTarea_WhenTareaNotFound_ReturnsNull()
    {
        var handler = new CompletarTareaHandler(new FakeTareaRepository(), new FakeGamificationUnlockMaterializer());

        var result = await handler.Handle(
            new CompletarTareaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteTarea_WhenTareaExists_ReturnsTrue()
    {
        var repo = new FakeTareaRepository { DeleteResult = true };
        var handler = new DeleteTareaHandler(repo);

        var result = await handler.Handle(
            new DeleteTareaCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteTarea_WhenTareaNotFound_ReturnsFalse()
    {
        var repo = new FakeTareaRepository { DeleteResult = false };
        var handler = new DeleteTareaHandler(repo);

        var result = await handler.Handle(
            new DeleteTareaCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task AsignarTarea_WhenTareaExists_ReturnsAssignedTarea()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.TareaAsignada = MakeTarea(hogarId, "Aspirar");
        var handler = new AsignarTareaHandler(repo, new AllowAllMembershipService());

        var result = await handler.Handle(
            new AsignarTareaCommand(Guid.NewGuid(), hogarId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Aspirar", result!.Titulo);
    }

    [Fact]
    public async Task AsignarTarea_WhenTareaNotFound_ReturnsNull()
    {
        var handler = new AsignarTareaHandler(new FakeTareaRepository(), new AllowAllMembershipService());

        var result = await handler.Handle(
            new AsignarTareaCommand(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AsignarTarea_WhenAsignadoAIsNull_DoesNotInvokeMembershipService()
    {
        var repo = new FakeTareaRepository();
        var membership = new AllowAllMembershipService();
        var handler = new AsignarTareaHandler(repo, membership);

        await handler.Handle(
            new AsignarTareaCommand(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(0, membership.EnsureMemberCallCount);
        Assert.Equal(1, repo.AsignarCallCount);
    }

    [Fact]
    public async Task AsignarTarea_WhenAsignadoANotMemberOfHousehold_ThrowsAndDoesNotCallRepository()
    {
        var repo = new FakeTareaRepository();
        var handler = new AsignarTareaHandler(repo, new DenyAllMembershipService());
        var hogarId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AsignarTareaCommand(Guid.NewGuid(), hogarId, Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(0, repo.AsignarCallCount);
    }

    [Fact]
    public async Task AsignarTarea_WhenAsignadoAIsMemberOfHousehold_AssignsTarea()
    {
        var hogarId = Guid.NewGuid();
        var asignadoA = Guid.NewGuid();
        var membership = new SelectiveMembershipService(allow: asignadoA);
        var repo = new FakeTareaRepository();
        repo.TareaAsignada = MakeTarea(hogarId, "Asignada OK");
        var handler = new AsignarTareaHandler(repo, membership);

        var result = await handler.Handle(
            new AsignarTareaCommand(Guid.NewGuid(), hogarId, asignadoA, Guid.NewGuid()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, membership.EnsureMemberCallCount);
        Assert.Equal(1, repo.AsignarCallCount);
    }

    [Fact]
    public async Task GetDistribucionSemanal_PassesOffsetMinutesToRepo()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeTareaRepository();
        repo.Distribucion = [
            new DistribucionDiaResult("Lun", DateTime.UtcNow, []),
            new DistribucionDiaResult("Mar", DateTime.UtcNow.AddDays(1), []),
        ];
        var handler = new GetDistribucionSemanalHandler(repo);

        var result = await handler.Handle(
            new GetDistribucionSemanalQuery(hogarId, -180),
            CancellationToken.None);

        Assert.Equal(hogarId, repo.LastDistribucionHogarId);
        Assert.Equal(-180, repo.LastDistribucionOffset);
        Assert.Equal(2, result.Count);
    }

    private static TareaResult MakeTarea(Guid hogarId, string titulo, string estado = "pendiente") =>
        new(Guid.NewGuid(), hogarId, titulo, null, estado,
            null, null, Guid.NewGuid(), "Test User", null, null, null, DateTime.UtcNow);

    private sealed class FakeTareaRepository : ITareaRepository
    {
        public List<TareaResult> TareasDeHogar { get; set; } = [];
        public List<TareaResult> TareasAsignadas { get; set; } = [];
        public TareaResult? TareaToReturn { get; set; }
        public TareaResult? TareaCreada { get; set; }
        public TareaResult? TareaCompletada { get; set; }
        public TareaResult? TareaAsignada { get; set; }
        public bool DeleteResult { get; set; }
        public List<DistribucionDiaResult> Distribucion { get; set; } = [];

        public Guid LastGetByAsignadoHogarId { get; private set; }
        public Guid LastGetByAsignadoUsuarioId { get; private set; }
        public Guid LastCreateHogarId { get; private set; }
        public Guid LastCreateCreadoPor { get; private set; }
        public string? LastCreateTitulo { get; private set; }
        public string? LastCreateDescripcion { get; private set; }
        public DateTime? LastCreateFechaLimite { get; private set; }
        public Guid? LastCreateAsignadoA { get; private set; }
        public Guid LastDistribucionHogarId { get; private set; }
        public int LastDistribucionOffset { get; private set; }
        public int CreateCallCount { get; private set; }
        public int AsignarCallCount { get; private set; }

        public Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct) =>
            Task.FromResult(TareasDeHogar);

        public Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
        {
            LastGetByAsignadoHogarId = hogarId;
            LastGetByAsignadoUsuarioId = usuarioId;
            return Task.FromResult(TareasAsignadas);
        }

        public Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct) =>
            Task.FromResult(TareaToReturn);

        public Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion,
            DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct)
        {
            CreateCallCount++;
            LastCreateHogarId = hogarId;
            LastCreateCreadoPor = creadoPor;
            LastCreateTitulo = titulo;
            LastCreateDescripcion = descripcion;
            LastCreateFechaLimite = fechaLimite;
            LastCreateAsignadoA = asignadoA;
            return Task.FromResult(TareaCreada!);
        }

        public Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion,
            DateTime? fechaLimite, string? estado, CancellationToken ct) =>
            Task.FromResult(TareaToReturn);

        public Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct) =>
            Task.FromResult(TareaCompletada);

        public Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct)
        {
            AsignarCallCount++;
            return Task.FromResult(TareaAsignada);
        }

        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct) =>
            Task.FromResult(DeleteResult);

        public Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct)
        {
            LastDistribucionHogarId = hogarId;
            LastDistribucionOffset = utcOffsetMinutes;
            return Task.FromResult(Distribucion);
        }
    }

    private sealed class AllowAllMembershipService : IHouseholdMembershipService
    {
        public int EnsureMemberCallCount { get; private set; }

        public Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        {
            EnsureMemberCallCount++;
            return Task.CompletedTask;
        }
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct)
        {
            EnsureMemberCallCount++;
            return Task.CompletedTask;
        }
        public Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class DenyAllMembershipService : IHouseholdMembershipService
    {
        public Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => throw new InvalidOperationException();
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => throw new InvalidOperationException();
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct) => throw deniedFactory();
        public Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct) => throw new InvalidOperationException();
    }

    private sealed class SelectiveMembershipService : IHouseholdMembershipService
    {
        private readonly Guid _allow;
        public int EnsureMemberCallCount { get; private set; }

        public SelectiveMembershipService(Guid allow)
        {
            _allow = allow;
        }

        public Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        {
            EnsureMemberCallCount++;
            if (usuarioId != _allow) throw new InvalidOperationException();
            return Task.CompletedTask;
        }
        public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct)
        {
            EnsureMemberCallCount++;
            if (usuarioId != _allow) throw deniedFactory();
            return Task.CompletedTask;
        }
        public Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct) => Task.CompletedTask;
    }
}
