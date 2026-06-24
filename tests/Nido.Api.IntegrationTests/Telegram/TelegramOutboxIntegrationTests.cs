using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Telegram.Client;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Telegram;

[Collection("TelegramWebhook")]
public sealed class TelegramOutboxIntegrationTests : IClassFixture<NidoTestWebAppFactory>
{
    private const string Secret = "default-test-webhook-secret";
    private readonly NidoTestWebAppFactory _factory;

    public TelegramOutboxIntegrationTests(NidoTestWebAppFactory factory)
    {
        var client = new FakeTelegramClient();
        _factory = factory.WithStorageOverride(services =>
        {
            services.RemoveAll<ITelegramClient>();
            services.AddSingleton<ITelegramClient>(client);
        }).WithTelegramWebhookConfig(Secret);

        TelegramClient = client;
    }

    private FakeTelegramClient TelegramClient { get; }

    [Fact]
    public async Task Webhook_MenuReply_IsDeliveredBySenderWorker()
    {
        await SeedLinkedCurrentMemberAsync(_factory, 901);

        using var client = _factory.CreateClient();
        using var content = new StringContent(BuildUpdateBody(90_001, 901, "/menu"), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/telegram") { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sent = await WaitForSentStatusAsync(_factory, 901, TimeSpan.FromSeconds(10));
        Assert.True(sent);
        Assert.Contains(TelegramClient.Messages, message => message.ChatId == 901);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var row = await db.TelegramOutboxMessages.AsNoTracking().SingleAsync(x => x.ChatId == 901);
        Assert.Equal(1, row.Attempts);
    }

    private static async Task<bool> WaitForSentStatusAsync(NidoTestWebAppFactory factory, long chatId, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var sent = await db.TelegramOutboxMessages.AsNoTracking()
                .AnyAsync(x => x.ChatId == chatId && x.Status == 2);

            if (sent)
            {
                return true;
            }

            await Task.Delay(250);
        }

        return false;
    }

    private static async Task SeedLinkedCurrentMemberAsync(NidoTestWebAppFactory factory, long chatId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Telegram",
            Email = $"telegram-worker-{chatId}@example.com",
            PasswordHash = "hash",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Hogares.Add(new Hogare { Id = hogarId, Nombre = $"Hogar {chatId}", CreatedAt = DateTime.UtcNow });
        db.MiembrosHogars.Add(new MiembrosHogar { Id = Guid.NewGuid(), UsuarioId = usuarioId, HogarId = hogarId, Rol = "owner", Puntos = 0 });
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

    private static string BuildUpdateBody(long updateId, long chatId, string text)
        => "{\"update_id\":" + updateId + ",\"message\":{\"message_id\":1,\"date\":1,\"text\":"
            + System.Text.Json.JsonSerializer.Serialize(text)
            + ",\"chat\":{\"id\":" + chatId + ",\"type\":\"private\"}}}";

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
