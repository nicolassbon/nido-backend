using Nido.Application.Auth.Interfaces;
using Nido.Application.Hogares.Exceptions;

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
            throw new MissingInvitationTokenException();

        var invitacion = await _repository.GetInvitacionByTokenAsync(command.Token, ct);

        if (invitacion is null)
            throw new InvitationNotFoundException();

        if (invitacion.Estado != "pendiente")
            throw new InvitationAlreadyProcessedException();

        if (invitacion.ExpiraEn.HasValue && invitacion.ExpiraEn.Value < DateTime.UtcNow)
            throw new InvitationExpiredException();

        var totalMiembros = await _repository.CountRealMembersAsync(invitacion.HogarId, ct);
        if (totalMiembros >= MaxMiembros)
            throw new MaxMembersExceededException(MaxMiembros);

        if (await _repository.IsMemberOfHouseholdAsync(command.UsuarioId, invitacion.HogarId, ct))
            throw new AlreadyMemberException();

        await _repository.AddUserToHouseholdAsync(command.UsuarioId, invitacion.HogarId, command.Token, ct);

        var (email, nombre) = await _repository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var nuevoToken = _jwtTokenService.CreateToken(command.UsuarioId, invitacion.HogarId, email, nombre);

        return new AceptarInvitacionResult(invitacion.HogarId, invitacion.HogarNombre, nuevoToken);
    }
}
