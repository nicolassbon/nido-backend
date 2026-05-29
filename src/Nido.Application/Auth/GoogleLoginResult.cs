namespace Nido.Application.Auth;

public sealed record GoogleLoginResult(Guid UsuarioId, Guid HogarId, string AccessToken, bool IsNewUser, string? RefreshToken = null);
