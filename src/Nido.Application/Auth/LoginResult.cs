namespace Nido.Application.Auth;

public sealed record LoginResult(Guid UsuarioId, Guid HogarId, string AccessToken, string? RefreshToken = null);
