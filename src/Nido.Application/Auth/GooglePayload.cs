namespace Nido.Application.Auth;

public sealed record GooglePayload(
    string Email,
    string GoogleId);
