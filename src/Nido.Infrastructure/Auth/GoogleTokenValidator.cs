using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Nido.Application.Auth;

namespace Nido.Infrastructure.Auth;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Google:ClientId is not configured. Set the 'Google__ClientId' environment variable or 'Google:ClientId' in appsettings.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new GooglePayload(payload.Email, payload.Subject);
    }
}
