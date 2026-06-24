namespace Nido.Application.Planificador;

public interface IPlanificadorRepository
{
    Task<PlanificadorSemanaResult> GetOrCreateSemanaAsync(Guid hogarId, DateOnly fechaInicio, CancellationToken ct);
    Task<PlanificadorItemResult> AddItemAsync(AddPlanificadorItemCommand command, CancellationToken ct);
    Task<PlanificadorItemResult?> UpdateItemAsync(UpdatePlanificadorItemCommand command, CancellationToken ct);
    Task<bool> DeleteItemAsync(DeletePlanificadorItemCommand command, CancellationToken ct);
}
