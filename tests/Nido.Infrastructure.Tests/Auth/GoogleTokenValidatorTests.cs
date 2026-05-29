using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Nido.Application.Auth;
using Nido.Infrastructure.Auth;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class GoogleTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_InvalidToken_ThrowsInvalidJwtException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = "test-client-id" })
            .Build();
        var validator = new GoogleTokenValidator(config);

        var exception = await Assert.ThrowsAsync<InvalidJwtException>(() => validator.ValidateAsync("invalid-token", CancellationToken.None));
    }
}
