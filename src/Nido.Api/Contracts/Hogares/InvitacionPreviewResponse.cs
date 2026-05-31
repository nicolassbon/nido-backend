namespace Nido.Api.Contracts.Hogares;

public sealed record InvitacionPreviewResponse(string HogarNombre, string? EmailInvitado, DateTime? ExpiraEn);
