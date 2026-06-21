namespace Nido.Api.Contracts.Hogares;

public sealed record CambiarHogarResponse(Guid HogarId, string HogarNombre, string AccessToken);
