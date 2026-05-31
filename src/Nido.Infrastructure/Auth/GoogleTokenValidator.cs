using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Nido.Application.Auth;

namespace Nido.Infrastructure.Auth;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleOptions _googleOptions;

    public GoogleTokenValidator(IOptions<GoogleOptions> googleOptions)
    {
        _googleOptions = googleOptions.Value;
    }

    public async Task<GooglePayload> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_googleOptions.ClientId]
        };
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new GooglePayload(payload.Email, payload.Subject);
    }
}
