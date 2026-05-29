namespace Nido.Api.Contracts.Auth;

public sealed record LinkGoogleRequest(string IdToken, string Password);
