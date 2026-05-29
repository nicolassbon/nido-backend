namespace Nido.Application.Auth;

public sealed record LinkGoogleCommand(string IdToken, string Password);
