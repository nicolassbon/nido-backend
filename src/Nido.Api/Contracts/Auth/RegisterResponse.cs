namespace Nido.Api.Contracts.Auth;

public sealed record RegisterResponse(Guid UsuarioId, Guid HogarId, string AccessToken);
