using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Nido.Infrastructure.Auth;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class GoogleTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_InvalidToken_ThrowsInvalidJwtException()
    {
        var validator = new GoogleTokenValidator(Options.Create(new GoogleOptions
        {
            ClientId = "test-client-id"
        }));

        await Assert.ThrowsAsync<InvalidJwtException>(() => validator.ValidateAsync("invalid-token", CancellationToken.None));
    }
}
