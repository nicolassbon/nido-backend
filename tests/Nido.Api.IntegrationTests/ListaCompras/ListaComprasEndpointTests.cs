using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.ListaCompras;

public sealed class ListaComprasEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public ListaComprasEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WhenAnonymous_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/lista-compras");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GroupFlow_PreservesOrderAndPurchasedHistory()
    {
        var user = await RegisterAndAuthenticateAsync(_client, "lista-flow");

        var createResponse = await _client.PostAsJsonAsync("/api/lista-compras/grupos", new
        {
            grupoNombre = "Tarta de verduras",
            items = new[]
            {
                new { nombre = "Harina", cantidad = (decimal?)500m, unidad = "g", grupoNombre = (string?)null },
                new { nombre = "Acelga", cantidad = (decimal?)1m, unidad = "atado", grupoNombre = (string?)null }
            }
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var groups = await createResponse.Content.ReadFromJsonAsync<List<ListaGrupoBody>>();
        var group = Assert.Single(groups!);
        Assert.Equal("Tarta de verduras", group.GrupoNombre);
        Assert.Equal(["Harina", "Acelga"], group.Items.Select(item => item.Nombre).ToArray());

        var purchasedId = group.Items[1].Id;
        var markResponse = await _client.PatchAsJsonAsync($"/api/lista-compras/items/{purchasedId}/comprado", new { });
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);
        var marked = await markResponse.Content.ReadFromJsonAsync<ListaItemBody>();
        Assert.True(marked!.Comprado);
        Assert.NotNull(marked.CompradoEn);

        var activeResponse = await _client.GetAsync("/api/lista-compras");
        var activeGroups = await activeResponse.Content.ReadFromJsonAsync<List<ListaGrupoBody>>();
        var activeItems = Assert.Single(activeGroups!).Items;
        Assert.Equal(["Harina", "Acelga"], activeItems.Select(item => item.Nombre).ToArray());
        Assert.True(activeItems.Single(item => item.Id == purchasedId).Comprado);

        var historyResponse = await _client.GetAsync("/api/lista-compras/historial");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<HistorialItemBody>>();
        var historyItem = Assert.Single(history!);
        Assert.Equal("Acelga", historyItem.Nombre);
        Assert.Equal("Tarta de verduras", historyItem.GrupoNombre);
        Assert.Equal(user.UsuarioId, historyItem.CompradoPor);
    }

    [Fact]
    public async Task RemoveAndClear_HideActiveItemsButKeepPurchasedHistory()
    {
        await RegisterAndAuthenticateAsync(_client, "lista-clear");

        var manualResponse = await _client.PostAsJsonAsync("/api/lista-compras/items", new
        {
            nombre = "Leche",
            cantidad = (decimal?)1m,
            unidad = "lt",
            grupoNombre = (string?)null
        });
        var manual = await manualResponse.Content.ReadFromJsonAsync<ListaItemBody>();

        var purchasedResponse = await _client.PostAsJsonAsync("/api/lista-compras/items", new
        {
            nombre = "Pan",
            cantidad = (decimal?)2m,
            unidad = "unidad",
            grupoNombre = (string?)null
        });
        var purchased = await purchasedResponse.Content.ReadFromJsonAsync<ListaItemBody>();

        var markResponse = await _client.PatchAsJsonAsync($"/api/lista-compras/items/{purchased!.Id}/comprado", new { });
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        var removeResponse = await _client.DeleteAsync($"/api/lista-compras/items/{purchased.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var clearResponse = await _client.DeleteAsync("/api/lista-compras");
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var activeResponse = await _client.GetAsync("/api/lista-compras");
        var activeGroups = await activeResponse.Content.ReadFromJsonAsync<List<ListaGrupoBody>>();
        Assert.Empty(activeGroups!);

        var historyResponse = await _client.GetAsync("/api/lista-compras/historial");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<HistorialItemBody>>();
        var historyItem = Assert.Single(history!);
        Assert.Equal("Pan", historyItem.Nombre);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var pending = await db.ListaCompras.SingleAsync(item => item.Id == manual!.Id);
        Assert.NotNull(pending.RemovidoDeListaAt);
    }

    private static async Task<RegisterBody> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return body;
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ListaGrupoBody(string GrupoNombre, List<ListaItemBody> Items);
    private sealed record ListaItemBody(Guid Id, Guid ProductoId, string Nombre, decimal? Cantidad, string? Unidad, bool Comprado, DateTime? CompradoEn, int Orden);
    private sealed record HistorialItemBody(Guid Id, Guid ProductoId, string Nombre, decimal? Cantidad, string? Unidad, string GrupoNombre, DateTime CompradoEn, Guid? CompradoPor);
}

