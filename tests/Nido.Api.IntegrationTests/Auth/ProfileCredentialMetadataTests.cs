using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class ProfileCredentialMetadataTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public ProfileCredentialMetadataTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PerfilEndpoint_ReturnsCredentialFlagsMatrix(bool hasPassword, bool hasGoogleLinked)
    {
        var client = _factory.CreateClient();
        var email = $"perfil-{Guid.NewGuid()}@test.com";
        string token;
        const string seedPassword = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            Guid userId;
            Guid hogarId;
            if (hasGoogleLinked)
            {
                (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Perfil User", email, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
                if (hasPassword)
                {
                    await repo.UpdateUserPasswordAsync(userId, hasher.Hash(seedPassword), CancellationToken.None);
                }
            }
            else
            {
                (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Perfil User", email, hasher.Hash(seedPassword), "M", null, CancellationToken.None);
            }

            token = tokenService.CreateToken(userId, hogarId, email, "Perfil User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/perfiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PerfilBody>();
        Assert.NotNull(body);
        Assert.Equal(hasPassword, body!.HasPassword);
        Assert.Equal(hasGoogleLinked, body.HasGoogleLinked);
    }

    private sealed record PerfilBody(bool HasPassword, bool HasGoogleLinked);
}
