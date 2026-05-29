namespace Nido.Application.Hogares;

public sealed class InvitarConviventeHandler
{
    private const int MaxMiembros = 6;
    private readonly IInvitacionRepository _repository;
    private readonly IEmailService _emailService;

    public InvitarConviventeHandler(IInvitacionRepository repository, IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }

    public async Task<string> Handle(InvitarConviventeCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.EmailInvitado))
            throw new ArgumentException("El email del invitado es requerido.");

        if (!await _repository.IsUserHouseholdOwnerAsync(command.UsuarioId, command.HogarId, ct))
            throw new UnauthorizedAccessException("Solo el propietario puede invitar convivientes.");

        var totalMiembros = await _repository.CountRealMembersAsync(command.HogarId, ct);
        if (totalMiembros >= MaxMiembros)
            throw new InvalidOperationException($"El hogar ya alcanzó el límite de {MaxMiembros} miembros.");

        var (_, ownerNombre) = await _repository.GetUsuarioInfoAsync(command.UsuarioId, ct);
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = await _repository.CreateInvitacionAsync(command.HogarId, command.UsuarioId, command.EmailInvitado, expiresAt, ct);

        var invitacion = await _repository.GetInvitacionByTokenAsync(token, ct);
        await _emailService.SendInvitationEmailAsync(command.EmailInvitado, invitacion!.HogarNombre, ownerNombre, token, ct);

        return token;
    }
}
