using Nido.Application.Common.Security;
using Nido.Application.Planificador;

namespace Nido.Application.Tests.Planificador;

public sealed class PlanificadorHandlerTests
{
    [Fact]
    public async Task GetSemana_WhenFechaIsSunday_UsesPreviousMonday()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository, new AllowAllMembershipService());
        var hogarId = Guid.NewGuid();

        await handler.GetSemana(hogarId, new DateOnly(2026, 6, 21), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 15), repository.LastFechaInicio);
    }

    [Fact]
    public async Task GetSemana_WhenFechaIsMonday_KeepsSameDate()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository, new AllowAllMembershipService());
        var hogarId = Guid.NewGuid();

        await handler.GetSemana(hogarId, new DateOnly(2026, 6, 15), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 15), repository.LastFechaInicio);
    }

    [Fact]
    public async Task AddItem_WhenAsignadoANotMemberOfHousehold_ThrowsAndDoesNotCallRepository()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository, new DenyAllMembershipService());
        var hogarId = Guid.NewGuid();
        var command = new AddPlanificadorItemCommand(
            hogarId, Guid.NewGuid(), new DateOnly(2026, 6, 19), "tarea",
            null, "Tarea", "10:00", Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.AddItem(command, CancellationToken.None));
        Assert.Equal(0, repository.AddItemCallCount);
    }

    [Fact]
    public async Task AddItem_WhenAsignadoAIsNull_DoesNotInvokeMembershipService()
    {
        var repository = new RecordingPlanificadorRepository();
        var membership = new AllowAllMembershipService();
        var handler = new PlanificadorHandler(repository, membership);
        var hogarId = Guid.NewGuid();
        var command = new AddPlanificadorItemCommand(
            hogarId, Guid.NewGuid(), new DateOnly(2026, 6, 19), "tarea",
            null, "Tarea", "10:00", null);

        await handler.AddItem(command, CancellationToken.None);
        Assert.Equal(0, membership.EnsureMemberCallCount);
        Assert.Equal(1, repository.AddItemCallCount);
    }

    [Fact]
    public async Task UpdateItem_WhenAsignadoANotMemberOfHousehold_ThrowsAndDoesNotCallRepository()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository, new DenyAllMembershipService());
        var hogarId = Guid.NewGuid();
        var command = new UpdatePlanificadorItemCommand(
            Guid.NewGuid(), hogarId, Guid.NewGuid(), null, "Tarea", "10:00", Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.UpdateItem(command, CancellationToken.None));
        Assert.Equal(0, repository.UpdateItemCallCount);
    }

    private sealed class RecordingPlanificadorRepository : IPlanificadorRepository
    {
        public DateOnly LastFechaInicio { get; private set; }
        public int AddItemCallCount { get; private set; }
        public int UpdateItemCallCount { get; private set; }

        public Task<PlanificadorSemanaResult> GetOrCreateSemanaAsync(Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
        {
            LastFechaInicio = fechaInicio;
            return Task.FromResult(new PlanificadorSemanaResult(Guid.NewGuid(), fechaInicio, []));
        }

        public Task<PlanificadorItemResult> AddItemAsync(AddPlanificadorItemCommand command, CancellationToken ct)
        {
            AddItemCallCount++;
            return Task.FromResult(new PlanificadorItemResult(
                Guid.NewGuid(), command.Fecha, command.TipoComida, null, null, null, null,
                command.TituloLibre, command.Hora, "pendiente", null, 0, command.UsuarioId));
        }

        public Task<PlanificadorItemResult?> UpdateItemAsync(UpdatePlanificadorItemCommand command, CancellationToken ct)
        {
            UpdateItemCallCount++;
            return Task.FromResult<PlanificadorItemResult?>(new PlanificadorItemResult(
                command.ItemId, new DateOnly(2026, 6, 19), "tarea", null, null, null, null,
                command.TituloLibre, command.Hora, "pendiente", null, 0, command.UsuarioId));
        }

        public Task<bool> DeleteItemAsync(DeletePlanificadorItemCommand command, CancellationToken ct)
            => throw new NotImplementedException();
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
}
