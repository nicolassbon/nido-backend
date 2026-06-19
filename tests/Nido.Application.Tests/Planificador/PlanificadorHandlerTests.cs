using Nido.Application.Planificador;

namespace Nido.Application.Tests.Planificador;

public sealed class PlanificadorHandlerTests
{
    [Fact]
    public async Task GetSemana_WhenFechaIsSunday_UsesPreviousMonday()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository);
        var hogarId = Guid.NewGuid();

        await handler.GetSemana(hogarId, new DateOnly(2026, 6, 21), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 15), repository.LastFechaInicio);
    }

    [Fact]
    public async Task GetSemana_WhenFechaIsMonday_KeepsSameDate()
    {
        var repository = new RecordingPlanificadorRepository();
        var handler = new PlanificadorHandler(repository);
        var hogarId = Guid.NewGuid();

        await handler.GetSemana(hogarId, new DateOnly(2026, 6, 15), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 15), repository.LastFechaInicio);
    }

    private sealed class RecordingPlanificadorRepository : IPlanificadorRepository
    {
        public DateOnly LastFechaInicio { get; private set; }

        public Task<PlanificadorSemanaResult> GetOrCreateSemanaAsync(Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
        {
            LastFechaInicio = fechaInicio;
            return Task.FromResult(new PlanificadorSemanaResult(Guid.NewGuid(), fechaInicio, []));
        }

        public Task<PlanificadorItemResult> AddItemAsync(AddPlanificadorItemCommand command, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<bool> DeleteItemAsync(DeletePlanificadorItemCommand command, CancellationToken ct)
            => throw new NotImplementedException();
    }
}
