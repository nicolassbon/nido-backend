using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests.Auth;

namespace Nido.Api.IntegrationTests.Preferencias;

public sealed class PreferenciasEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;

    public PreferenciasEndpointTests(NidoTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = $"prefs-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await _client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    [Fact]
    public async Task GetPreferencias_SinAutenticacion_Retorna401()
    {
        var response = await _client.GetAsync("/api/preferencias/usuario");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPreferencias_RecienRegistrado_RetornaDefaults()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/preferencias/usuario");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PrefsBody>();
        Assert.NotNull(body);
        Assert.Equal(7, body!.DiasAlerta);
        Assert.Equal("system", body.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_ConDiasValidos_RetornaValorActualizado()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { diasAlerta = 14 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PrefsBody>();
        Assert.NotNull(body);
        Assert.Equal(14, body!.DiasAlerta);
        Assert.Equal("system", body.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_ConTemaValido_RetornaTemaActualizado()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { temaPreferido = "dark" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PrefsBody>();
        Assert.NotNull(body);
        Assert.Equal(7, body!.DiasAlerta);
        Assert.Equal("dark", body.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_GetDespuesDeUpdate_RetornaValorPersistido()
    {
        await AuthenticateAsync();

        await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { diasAlerta = 21 });
        await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { temaPreferido = "light" });
        var response = await _client.GetAsync("/api/preferencias/usuario");

        var body = await response.Content.ReadFromJsonAsync<PrefsBody>();
        Assert.Equal(21, body!.DiasAlerta);
        Assert.Equal("light", body.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_ConDiasCero_RetornaBadRequest()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { diasAlerta = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePreferencias_ConTemaInvalido_RetornaBadRequest()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/preferencias/usuario", new { temaPreferido = "sepia" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record PrefsBody(int DiasAlerta, string TemaPreferido);
}
