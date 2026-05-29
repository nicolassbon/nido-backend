namespace Nido.Api.Contracts.Auth;

public sealed record GoogleLoginResponse(Guid UsuarioId, Guid HogarId, string AccessToken, bool IsNewUser);
