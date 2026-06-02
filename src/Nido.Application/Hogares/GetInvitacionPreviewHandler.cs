using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record GetInvitacionPreviewResult(string HogarNombre, string? EmailInvitado, DateTime? ExpiraEn);

public sealed class GetInvitacionPreviewHandler
{
    private readonly IInvitacionRepository _repository;

    public GetInvitacionPreviewHandler(IInvitacionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetInvitacionPreviewResult> Handle(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new MissingInvitationTokenException();

        var invitacion = await _repository.GetInvitacionByTokenAsync(token, ct);

        if (invitacion is null)
            throw new InvitationNotFoundException();

        if (invitacion.Estado != "pendiente")
            throw new InvitationAlreadyProcessedException();

        if (invitacion.ExpiraEn.HasValue && invitacion.ExpiraEn.Value < DateTime.UtcNow)
            throw new InvitationExpiredException();

        return new GetInvitacionPreviewResult(invitacion.HogarNombre, invitacion.EmailInvitado, invitacion.ExpiraEn);
    }
}
