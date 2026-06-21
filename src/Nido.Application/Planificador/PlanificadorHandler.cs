using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Planificador;

public sealed class PlanificadorHandler
{
    private readonly IPlanificadorRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;

    public PlanificadorHandler(IPlanificadorRepository repository, IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    /// <summary>Devuelve (o crea) la semana a partir del lunes indicado.</summary>
    public Task<PlanificadorSemanaResult> GetSemana(Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
    {
        // Aseguramos que siempre sea el lunes de esa semana
        var dayOfWeek = (int)fechaInicio.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var lunes = fechaInicio.AddDays(-offset);
        return _repository.GetOrCreateSemanaAsync(hogarId, lunes, ct);
    }

    public async Task<PlanificadorItemResult> AddItem(AddPlanificadorItemCommand command, CancellationToken ct)
    {
        if (command.AsignadoA.HasValue)
        {
            await _membershipService.EnsureMemberAsync(command.AsignadoA.Value, command.HogarId, ct);
        }

        return await _repository.AddItemAsync(command, ct);
    }

    public async Task<PlanificadorItemResult?> UpdateItem(UpdatePlanificadorItemCommand command, CancellationToken ct)
    {
        if (command.AsignadoA.HasValue)
        {
            await _membershipService.EnsureMemberAsync(command.AsignadoA.Value, command.HogarId, ct);
        }

        return await _repository.UpdateItemAsync(command, ct);
    }

    public Task<bool> DeleteItem(DeletePlanificadorItemCommand command, CancellationToken ct)
        => _repository.DeleteItemAsync(command, ct);
}
