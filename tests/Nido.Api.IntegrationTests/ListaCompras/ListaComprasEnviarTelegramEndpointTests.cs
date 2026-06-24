using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nido.Api.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.ListaCompras;

public sealed class ListaComprasEnviarTelegramEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public ListaComprasEnviarTelegramEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendTelegram_WhenAnonymous_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsync("/api/lista-compras/enviar-telegram", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendTelegram_WhenUserHasActiveLinkAndPendingItems_EnqueuesOutboxMessage()
    {
        var registered = await RegisterAndAuthenticateAsync(_client, "lista-telegram-send");
        await SeedTelegramLinkAsync(registered.UsuarioId, registered.HogarId, 123_456_789L);

        var createResponse = await _client.PostAsJsonAsync("/api/listas-compra", new { nombre = "Compra semanal" });
        var created = await createResponse.Content.ReadFromJsonAsync<ListaBody>();

        var addResponse = await _client.PostAsJsonAsync($"/api/listas-compra/{created!.Id}/items", new
        {
            nombre = "Leche *light* [1L]",
            cantidad = (decimal?)1m,
            unidad = "lt"
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var response = await _client.PostAsync("/api/lista-compras/enviar-telegram?listaId=" + created.Id, new StringContent("{}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SendTelegramResponse>();
        Assert.Equal("enqueued", body!.Status);
        Assert.Equal(1, body.ItemCount);
        Assert.Equal(123_456_789L, body.ChatId);
        Assert.Equal(created.Id, body.ListaId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var outbox = await db.TelegramOutboxMessages.AsNoTracking().SingleAsync(x => x.ChatId == 123_456_789L);
        Assert.Equal("lista_compras", outbox.MessageType);
        Assert.Equal(0, outbox.Status);

        using var payload = JsonDocument.Parse(outbox.PayloadJson);
        Assert.Contains("Leche \\*light\\* \\[1L\\]", payload.RootElement.GetProperty("text").GetString());
        Assert.Equal("MarkdownV2", payload.RootElement.GetProperty("parse_mode").GetString());
    }

    [Fact]
    public async Task SendTelegram_WhenLinkMissing_Returns409AndDoesNotCreateOutboxRow()
    {
        await RegisterAndAuthenticateAsync(_client, "lista-telegram-missing-link");

        var createResponse = await _client.PostAsJsonAsync("/api/listas-compra", new { nombre = "Compra semanal" });
        var created = await createResponse.Content.ReadFromJsonAsync<ListaBody>();
        await _client.PostAsJsonAsync($"/api/listas-compra/{created!.Id}/items", new
        {
            nombre = "Arroz",
            cantidad = (decimal?)1m,
            unidad = "kg"
        });

        var response = await _client.PostAsync("/api/lista-compras/enviar-telegram", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var outboxCount = await db.TelegramOutboxMessages.CountAsync();
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task SendTelegram_WhenOtherHouseholdHasPendingItems_ExcludesThemFromPayload()
    {
        // Household A: user A with items
        var registeredA = await RegisterAndAuthenticateAsync(_client, "lista-telegram-isolation-A");
        await SeedTelegramLinkAsync(registeredA.UsuarioId, registeredA.HogarId, 333_333_333L);
        var createA = await _client.PostAsJsonAsync("/api/listas-compra", new { nombre = "Compra A" });
        var listA = await createA.Content.ReadFromJsonAsync<ListaBody>();
        await _client.PostAsJsonAsync($"/api/listas-compra/{listA!.Id}/items", new
        {
            nombre = "Item de A",
            cantidad = (decimal?)1m,
            unidad = "u"
        });

        using var clientB = _factory.CreateClient();
        var registeredB = await RegisterAndAuthenticateAsync(clientB, "lista-telegram-isolation-B");
        await SeedTelegramLinkAsync(registeredB.UsuarioId, registeredB.HogarId, 444_444_444L);
        var createB = await clientB.PostAsJsonAsync("/api/listas-compra", new { nombre = "Compra B" });
        var listB = await createB.Content.ReadFromJsonAsync<ListaBody>();

        var response = await clientB.PostAsync("/api/lista-compras/enviar-telegram?listaId=" + listB!.Id, new StringContent("{}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SendTelegramResponse>();
        Assert.Equal("empty", body!.Status);
        Assert.Equal(0, body.ItemCount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var outboxForB = await db.TelegramOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.ChatId == 444_444_444L);
        Assert.Null(outboxForB);
    }

    [Fact]
    public async Task SendTelegram_WhenListIsEmpty_Returns200EmptyWithoutOutboxRow()
    {
        var registered = await RegisterAndAuthenticateAsync(_client, "lista-telegram-empty");
        await SeedTelegramLinkAsync(registered.UsuarioId, registered.HogarId, 222_222_222L);

        var createResponse = await _client.PostAsJsonAsync("/api/listas-compra", new { nombre = "Vacía" });
        var created = await createResponse.Content.ReadFromJsonAsync<ListaBody>();

        var response = await _client.PostAsync("/api/lista-compras/enviar-telegram?listaId=" + created!.Id, new StringContent("{}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SendTelegramResponse>();
        Assert.Equal("empty", body!.Status);
        Assert.Equal(0, body.ItemCount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var outboxCount = await db.TelegramOutboxMessages.CountAsync(x => x.ChatId == 222_222_222L);
        Assert.Equal(0, outboxCount);
    }

    private static async Task<RegisterBody> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var register = await client.PostAsync("/api/auth/register", registerContent);
        var body = await register.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return body;
    }

    private async Task SeedTelegramLinkAsync(Guid usuarioId, Guid hogarId, long chatId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        db.TelegramChatLinks.Add(new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UsuarioId = usuarioId,
            HogarId = hogarId,
            PairedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record ListaBody(Guid Id, string Nombre, DateTime CreatedAt, DateTime? UpdatedAt, List<ListaItemBody> Items);
    private sealed record ListaItemBody(Guid Id, Guid? ProductoId, string Nombre, decimal? Cantidad, string? Unidad, bool Comprado, DateTime? CompradoEn, int Orden);
    private sealed record SendTelegramResponse(string Status, int ItemCount, long? ChatId, Guid? ListaId);
}
