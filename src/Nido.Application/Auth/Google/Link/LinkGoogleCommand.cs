namespace Nido.Application.Auth.Google.Link;

public sealed record LinkGoogleCommand(Guid UserId, string IdToken);
