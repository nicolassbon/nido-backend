namespace Nido.Application.Auth.Register;

public sealed record RegisterUserResult(
    Guid? UsuarioId,
    Guid? HogarId,
    string? AccessToken,
    string? RefreshToken,
    bool IsSilentSuccess)
{
    public static RegisterUserResult Created(Guid usuarioId, Guid hogarId, string accessToken, string? refreshToken)
        => new(usuarioId, hogarId, accessToken, refreshToken, false);

    public static RegisterUserResult SilentSuccess()
        => new(null, null, null, null, true);
}
