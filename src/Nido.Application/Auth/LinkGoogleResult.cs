namespace Nido.Application.Auth;

public sealed record LinkGoogleResult(Guid UsuarioId, Guid HogarId, string AccessToken, string? RefreshToken = null);
