namespace Nido.Application.Auth.Google.Link;

public sealed record LinkGoogleResult(Guid UsuarioId, Guid HogarId, string AccessToken, string? RefreshToken = null);
