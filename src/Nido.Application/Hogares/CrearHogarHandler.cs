using Nido.Application.Auth.Interfaces;

namespace Nido.Application.Hogares;

public sealed record CrearHogarCommand(Guid UsuarioId, string Nombre);
public sealed record CrearHogarResult(Guid HogarId, string HogarNombre, string NuevoToken);

public sealed class CrearHogarHandler
{
    private readonly IHogarRepository _hogarRepository;
    private readonly IInvitacionRepository _invitacionRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public CrearHogarHandler(IHogarRepository hogarRepository, IInvitacionRepository invitacionRepository, IJwtTokenService jwtTokenService)
    {
        _hogarRepository = hogarRepository;
        _invitacionRepository = invitacionRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<CrearHogarResult> Handle(CrearHogarCommand command, CancellationToken ct)
    {
        var nombre = command.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 80)
            throw new ArgumentException("El nombre del hogar debe tener entre 1 y 80 caracteres.");

        var hogar = await _hogarRepository.CreateHogarAsync(command.UsuarioId, nombre, ct);
        var (email, nombreUsuario) = await _invitacionRepository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var nuevoToken = _jwtTokenService.CreateToken(command.UsuarioId, hogar.Id, email, nombreUsuario);

        return new CrearHogarResult(hogar.Id, hogar.Nombre, nuevoToken);
    }
}
