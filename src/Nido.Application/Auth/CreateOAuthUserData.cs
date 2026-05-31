namespace Nido.Application.Auth;

public record CreateOAuthUserData(
        Guid UsuarioId,
        Guid HogarId,
        string Nombre,
        string Email,
        string OauthProvider,
        string OauthId);