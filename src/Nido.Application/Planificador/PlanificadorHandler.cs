namespace Nido.Application.Planificador;

public sealed class PlanificadorHandler
{
    private readonly IPlanificadorRepository _repository;

    public PlanificadorHandler(IPlanificadorRepository repository)
        => _repository = repository;

    /// <summary>Devuelve (o crea) la semana a partir del lunes indicado.</summary>
    public Task<PlanificadorSemanaResult> GetSemana(Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
    {
        // Aseguramos que siempre sea el lunes de esa semana
        var dayOfWeek = (int)fechaInicio.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var lunes = fechaInicio.AddDays(-offset);
        return _repository.GetOrCreateSemanaAsync(hogarId, lunes, ct);
    }

    public Task<PlanificadorItemResult> AddItem(AddPlanificadorItemCommand command, CancellationToken ct)
        => _repository.AddItemAsync(command, ct);

    public Task<PlanificadorItemResult?> UpdateItem(UpdatePlanificadorItemCommand command, CancellationToken ct)
        => _repository.UpdateItemAsync(command, ct);

    public Task<bool> DeleteItem(DeletePlanificadorItemCommand command, CancellationToken ct)
        => _repository.DeleteItemAsync(command, ct);
}
