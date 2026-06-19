using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nido.Application.Common.Security;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Client;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Xunit;

namespace Nido.Api.IntegrationTests.Telegram;

[Collection("TelegramWebhook")]
public sealed class TelegramPairingEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private const string WebhookEndpoint = "/api/webhooks/telegram";
    private const string StartEndpoint = "/api/telegram/pairing/start";

    private readonly NidoTestWebAppFactory _baseFactory;

    public TelegramPairingEndpointTests(NidoTestWebAppFactory baseFactory)
    {
        _baseFactory = baseFactory;
    }

    [Fact]
    public async Task StartEndpoint_ReturnsDeepLink_AndPersistsOnlyHashedToken()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(usuarioId, hogarId);

        using var factory = _baseFactory.WithStorageOverride(services =>
        {
            services.RemoveAll<ICurrentUserContext>();
            services.AddScoped<ICurrentUserContext>(_ => currentUser);
            services.PostConfigure<TelegramOptions>(options => options.BotUsername = "nido_bot");
        });

        await SeedUserAndHouseholdAsync(factory, usuarioId, hogarId);

        using var client = CreateAuthenticatedClient(factory.CreateClient());
        var response = await client.PostAsync(StartEndpoint, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("https://t.me/", payload);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var token = await db.TelegramPairingTokens.AsNoTracking().SingleAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId);
        Assert.DoesNotContain(token.TokenHash, payload, StringComparison.Ordinal);
        Assert.Equal(64, token.TokenHash.Length);
    }

    [Fact]
    public async Task StartEndpoint_WhenBotUsernameEmpty_Returns503ServiceUnavailable()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(usuarioId, hogarId);

        using var factory = _baseFactory.WithStorageOverride(services =>
        {
            services.RemoveAll<ICurrentUserContext>();
            services.AddScoped<ICurrentUserContext>(_ => currentUser);
            services.PostConfigure<TelegramOptions>(options => options.BotUsername = string.Empty);
        });

        await SeedUserAndHouseholdAsync(factory, usuarioId, hogarId);

        using var client = CreateAuthenticatedClient(factory.CreateClient());
        var response = await client.PostAsync(StartEndpoint, content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("TELEGRAM_CONFIGURATION", payload);
    }

    [Fact]
    public async Task WebhookStart_WithValidToken_CreatesChatLink_AndSendsConfirmation()
    {
        var sentMessages = new FakeTelegramClient();
        using var factory = _baseFactory.WithStorageOverride(services =>
        {
            services.RemoveAll<ITelegramClient>();
            services.AddSingleton<ITelegramClient>(sentMessages);
        }).WithTelegramWebhookConfig("default-test-webhook-secret");

        var (tokenHash, rawToken) = await SeedTokenAsync(factory, activeMembership: true);

        using var client = factory.CreateClient();
        var response = await PostWebhookAsync(client, 30_001, 55, $"/start {rawToken}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var token = await db.TelegramPairingTokens.SingleAsync(x => x.TokenHash == tokenHash);
        var link = await db.TelegramChatLinks.SingleAsync(x => x.ChatId == 55);
        Assert.NotNull(token.ConsumedAt);
        Assert.Null(link.UnpairedAt);
        Assert.Contains(sentMessages.Messages, message => message.ChatId == 55);
    }

    [Fact]
    public async Task WebhookStart_WithUnknownToken_Returns404NotFound()
    {
        using var factory = _baseFactory.WithTelegramWebhookConfig("default-test-webhook-secret");
        using var client = factory.CreateClient();

        var response = await PostWebhookAsync(client, 30_010, 56, "/start definitely-unknown-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("TELEGRAM_PAIRING_TOKEN_NOT_FOUND", payload);
    }

    [Fact]
    public async Task WebhookUnlink_DeactivatesActiveChatLink()
    {
        var sentMessages = new FakeTelegramClient();
        using var factory = _baseFactory.WithStorageOverride(services =>
        {
            services.RemoveAll<ITelegramClient>();
            services.AddSingleton<ITelegramClient>(sentMessages);
        }).WithTelegramWebhookConfig("default-test-webhook-secret");

        await SeedActiveLinkAsync(factory, 77);

        using var client = factory.CreateClient();
        var response = await PostWebhookAsync(client, 30_002, 77, "/unlink");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var link = await db.TelegramChatLinks.SingleAsync(x => x.ChatId == 77);
        Assert.NotNull(link.UnpairedAt);
        Assert.Contains(sentMessages.Messages, message => message.ChatId == 77);
    }

    private static HttpClient CreateAuthenticatedClient(HttpClient client)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-test-jwt-key-minimum-32-bytes-long!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "nido-api-tests",
            audience: "nido-clients-tests",
            claims: [new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static async Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, long updateId, long chatId, string text)
    {
        var json = $"{{\"update_id\":{updateId},\"message\":{{\"message_id\":1,\"date\":1,\"text\":\"{text}\",\"chat\":{{\"id\":{chatId},\"type\":\"private\"}}}}}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "default-test-webhook-secret");
        return await client.SendAsync(request);
    }

    private static async Task SeedUserAndHouseholdAsync(NidoTestWebAppFactory factory, Guid usuarioId, Guid hogarId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Api User",
            Email = $"{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Hogares.Add(new Hogare { Id = hogarId, Nombre = "Api Hogar", CreatedAt = DateTime.UtcNow });
        db.MiembrosHogars.Add(new MiembrosHogar { Id = Guid.NewGuid(), UsuarioId = usuarioId, HogarId = hogarId, Rol = "owner", Puntos = 0 });
        await db.SaveChangesAsync();
    }

    private static async Task<(string TokenHash, string RawToken)> SeedTokenAsync(NidoTestWebAppFactory factory, bool activeMembership)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var rawToken = "abcd1234";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

        db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Webhook User",
            Email = $"{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Hogares.Add(new Hogare { Id = hogarId, Nombre = "Webhook Hogar", CreatedAt = DateTime.UtcNow });

        if (activeMembership)
        {
            db.MiembrosHogars.Add(new MiembrosHogar { Id = Guid.NewGuid(), UsuarioId = usuarioId, HogarId = hogarId, Rol = "owner", Puntos = 0 });
        }

        db.TelegramPairingTokens.Add(new TelegramPairingToken
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await db.SaveChangesAsync();

        return (tokenHash, rawToken);
    }

    private static async Task SeedActiveLinkAsync(NidoTestWebAppFactory factory, long chatId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Linked User",
            Email = $"{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Hogares.Add(new Hogare { Id = hogarId, Nombre = "Linked Hogar", CreatedAt = DateTime.UtcNow });
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

    private sealed record FakeCurrentUserContext(Guid UsuarioId, Guid HogarId) : ICurrentUserContext;

    private sealed class FakeTelegramClient : ITelegramClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];

        public Task<TelegramSendResult> SendMessageAsync(long chatId, string text, string? parseMode = null, TelegramInlineKeyboardMarkup? replyMarkup = null, CancellationToken ct = default)
        {
            Messages.Add((chatId, text));
            return Task.FromResult<TelegramSendResult>(new TelegramSendResult.Success(new TelegramMessageSent(1)));
        }
    }
}
