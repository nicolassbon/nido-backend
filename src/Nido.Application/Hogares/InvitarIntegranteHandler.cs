using Nido.Application.Common.Notifications;
using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed class InvitarIntegranteHandler
{
    private const int MaxMiembros = 6;
    private readonly IInvitacionRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;
    private readonly IEmailService _emailService;

    public InvitarIntegranteHandler(
        IInvitacionRepository repository,
        IHouseholdMembershipService membershipService,
        IEmailService emailService)
    {
        _repository = repository;
        _membershipService = membershipService;
        _emailService = emailService;
    }

    public async Task<string> Handle(InvitarIntegranteCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.EmailInvitado))
            throw new MissingInvitationTokenException();

        await _membershipService.EnsureOwnerAsync(command.UsuarioId, command.HogarId, ct);

        var totalMiembros = await _repository.CountRealMembersAsync(command.HogarId, ct);
        if (totalMiembros >= MaxMiembros)
            throw new MaxMembersExceededException(MaxMiembros);

        var (_, ownerNombre) = await _repository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = await _repository.CreateInvitacionAsync(command.HogarId, command.UsuarioId, command.EmailInvitado, expiresAt, ct);

        var invitacion = await _repository.GetInvitacionByTokenAsync(token, ct);
        await _emailService.SendInvitationEmailAsync(command.EmailInvitado, invitacion!.HogarNombre, ownerNombre, token, ct);

        return token;
    }
}
