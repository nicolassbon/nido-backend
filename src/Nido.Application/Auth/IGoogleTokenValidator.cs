namespace Nido.Application.Auth;

public interface IGoogleTokenValidator
{
    Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
