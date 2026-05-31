using Nido.Application.Auth.Google.Login;

namespace Nido.Application.Auth.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
