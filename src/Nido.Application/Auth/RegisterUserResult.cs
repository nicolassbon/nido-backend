namespace Nido.Application.Auth;

public sealed record RegisterUserResult(Guid UsuarioId, Guid HogarId, string AccessToken);
