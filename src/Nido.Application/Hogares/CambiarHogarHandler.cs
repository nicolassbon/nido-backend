using Nido.Application.Auth.Interfaces;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record CambiarHogarCommand(Guid UsuarioId, Guid HogarId);
public sealed record CambiarHogarResult(Guid HogarId, string HogarNombre, string NuevoToken);

public sealed class CambiarHogarHandler
{
    private readonly IInvitacionRepository _invitacionRepository;
    private readonly IHogarRepository _hogarRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public CambiarHogarHandler(IInvitacionRepository invitacionRepository, IHogarRepository hogarRepository, IJwtTokenService jwtTokenService)
    {
        _invitacionRepository = invitacionRepository;
        _hogarRepository = hogarRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<CambiarHogarResult> Handle(CambiarHogarCommand command, CancellationToken ct)
    {
        if (!await _invitacionRepository.IsMemberOfHouseholdAsync(command.UsuarioId, command.HogarId, ct))
            throw new NotHouseholdMemberException();

        var hogar = await _hogarRepository.GetByIdAsync(command.HogarId, ct);
        var (email, nombre) = await _invitacionRepository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var nuevoToken = _jwtTokenService.CreateToken(command.UsuarioId, command.HogarId, email, nombre);

        return new CambiarHogarResult(hogar!.Id, hogar.Nombre, nuevoToken);
    }
}
