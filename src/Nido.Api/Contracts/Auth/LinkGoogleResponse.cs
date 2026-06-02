namespace Nido.Api.Contracts.Auth;

public sealed record LinkGoogleResponse(Guid UsuarioId, Guid HogarId, string AccessToken);
