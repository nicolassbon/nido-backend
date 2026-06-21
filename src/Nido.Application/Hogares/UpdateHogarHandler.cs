using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record UpdateHogarCommand(Guid CallerUsuarioId, Guid HogarId, string Nombre);

public sealed class UpdateHogarHandler
{
    private const int MinNombreLength = 1;
    private const int MaxNombreLength = 80;

    private readonly IHogarRepository _hogarRepository;
    private readonly IInvitacionRepository _invitacionRepository;

    public UpdateHogarHandler(IHogarRepository hogarRepository, IInvitacionRepository invitacionRepository)
    {
        _hogarRepository = hogarRepository;
        _invitacionRepository = invitacionRepository;
    }

    public async Task<HogarInfo> Handle(UpdateHogarCommand command, CancellationToken ct)
    {
        if (!await _invitacionRepository.IsUserHouseholdOwnerAsync(command.CallerUsuarioId, command.HogarId, ct))
            throw new NotHouseholdOwnerException();

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
