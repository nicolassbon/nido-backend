namespace Nido.Application.Auth;

public sealed record LinkGoogleCommand(Guid UserId, string IdToken);
