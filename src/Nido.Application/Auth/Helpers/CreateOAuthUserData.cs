namespace Nido.Application.Auth.Helpers;

public record CreateOAuthUserData(
    Guid UsuarioId,
    Guid HogarId,
    string Nombre,
    string Email,
    string OauthProvider,
    string OauthId,
    string? FotoStorageKey = null);
