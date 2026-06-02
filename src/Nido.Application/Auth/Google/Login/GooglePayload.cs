namespace Nido.Application.Auth.Google.Login;

public sealed record GooglePayload(
    string Email,
    string GoogleId);
