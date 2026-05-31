namespace Nido.Application.Auth.Login;

public sealed record LoginResult(Guid UsuarioId, Guid HogarId, string AccessToken, string? RefreshToken = null);
