using Nido.Application.Auth;

namespace Nido.Application.Hogares;

public sealed class AceptarInvitacionHandler
{
    private const int MaxMiembros = 6;
    private readonly IInvitacionRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;

    public AceptarInvitacionHandler(IInvitacionRepository repository, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AceptarInvitacionResult> Handle(AceptarInvitacionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            throw new ArgumentException("El token de invitación es requerido.");

        var invitacion = await _repository.GetInvitacionByTokenAsync(command.Token, ct);

        if (invitacion is null)
            throw new ArgumentException("Invitación no encontrada o inválida.");

        if (invitacion.Estado != "pendiente")
            throw new InvalidOperationException("Esta invitación ya fue utilizada o está cancelada.");

        if (invitacion.ExpiraEn.HasValue && invitacion.ExpiraEn.Value < DateTime.UtcNow)
            throw new InvalidOperationException("La invitación ha expirado.");

        var totalMiembros = await _repository.CountRealMembersAsync(invitacion.HogarId, ct);
        if (totalMiembros >= MaxMiembros)
            throw new InvalidOperationException("El hogar ya alcanzó el límite de miembros.");

        // If user is already in the target household, nothing to do
        var currentHogarId = await _repository.GetUserCurrentHogarIdAsync(command.UsuarioId, ct);
        if (currentHogarId == invitacion.HogarId)
            throw new InvalidOperationException("Ya sos miembro de este hogar.");

        // User can only move if they're the sole owner of their current (auto-created) household
        if (!await _repository.IsUserSoleOwnerAsync(command.UsuarioId, ct))
            throw new InvalidOperationException("Ya pertenecés a un hogar con otros miembros. No podés unirte a otro hogar.");

        await _repository.MoveUserToHouseholdAsync(command.UsuarioId, currentHogarId, invitacion.HogarId, command.Token, ct);

        var (email, _) = await _repository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var nuevoToken = _jwtTokenService.CreateToken(command.UsuarioId, invitacion.HogarId, email);

        return new AceptarInvitacionResult(invitacion.HogarId, invitacion.HogarNombre, nuevoToken);
    }
}
