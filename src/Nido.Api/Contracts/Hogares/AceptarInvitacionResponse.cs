namespace Nido.Api.Contracts.Hogares;

public sealed record AceptarInvitacionResponse(Guid HogarId, string HogarNombre, string AccessToken);
