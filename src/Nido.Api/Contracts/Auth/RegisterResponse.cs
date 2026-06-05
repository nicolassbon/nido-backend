namespace Nido.Api.Contracts.Auth;

public sealed record RegisterResponse(
    Guid? UsuarioId,
    Guid? HogarId,
    string? AccessToken,
    string Message,
    bool IsSilentSuccess)
{
    public static RegisterResponse Created(Guid usuarioId, Guid hogarId, string accessToken)
        => new(usuarioId, hogarId, accessToken, "Registration completed.", false);

    public static RegisterResponse SilentSuccess()
        => new(null, null, null, "If an account already exists for that email, we've kept the response generic for security.", true);
}
