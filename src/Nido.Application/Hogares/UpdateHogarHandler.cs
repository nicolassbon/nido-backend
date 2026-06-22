using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record UpdateHogarCommand(Guid CallerUsuarioId, Guid HogarId, string Nombre);

public sealed class UpdateHogarHandler
{
    private const int MinNombreLength = 1;
    private const int MaxNombreLength = 80;

    private readonly IHogarRepository _hogarRepository;
    private readonly IHouseholdMembershipService _membershipService;

    public UpdateHogarHandler(IHogarRepository hogarRepository, IHouseholdMembershipService membershipService)
    {
        _hogarRepository = hogarRepository;
        _membershipService = membershipService;
    }

    public async Task<HogarInfo> Handle(UpdateHogarCommand command, CancellationToken ct)
    {
        await _membershipService.EnsureOwnerAsync(command.CallerUsuarioId, command.HogarId, ct);

        var nombre = command.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length < MinNombreLength || nombre.Length > MaxNombreLength)
            throw new ArgumentException(
                $"El nombre del hogar debe tener entre {MinNombreLength} y {MaxNombreLength} caracteres.",
                nameof(command));

        await _hogarRepository.UpdateNombreAsync(command.HogarId, nombre, ct);

        var updated = await _hogarRepository.GetByIdAsync(command.HogarId, ct);
        return updated!;
    }
}
