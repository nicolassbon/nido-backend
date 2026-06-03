using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Hogares;

public sealed class HogaresEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public HogaresEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMiembros_DevuelveAlergiasDelUsuario()
    {
        var registered = await AuthenticateAsync();
        var restriccionId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.RestriccionesCatalogo.Add(new RestriccionesCatalogo
            {
                Id = restriccionId,
                Nombre = "Gluten",
                Tipo = "alergia"
            });
            db.RestriccionesUsuarios.Add(new RestriccionesUsuario
            {
                UsuarioId = registered.UsuarioId,
                RestriccionId = restriccionId
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/hogares/miembros");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var miembros = await response.Content.ReadFromJsonAsync<List<MiembroBody>>();
        var miembro = Assert.Single(miembros!);
        Assert.Equal(registered.UsuarioId, miembro.UsuarioId);
        Assert.Contains("Gluten", miembro.Alergias);
    }

    private async Task<RegisterBody> AuthenticateAsync()
    {
        var email = $"hogar-{Guid.NewGuid():N}@test.com";
        using var req = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var res = await _client.PostAsync("/api/auth/register", req);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return body;
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record MiembroBody(Guid UsuarioId, string Nombre, List<string> Alergias);
}
