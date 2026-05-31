namespace Nido.Application.Auth.Google.Login;

public sealed record GoogleLoginResult(Guid UsuarioId, Guid HogarId, string AccessToken, bool IsNewUser, string? RefreshToken = null);
