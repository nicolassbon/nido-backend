namespace Nido.Api.Contracts.Auth;

public sealed record GoogleLoginResponse(string AccessToken, bool IsNewUser);
