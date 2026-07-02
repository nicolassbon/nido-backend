using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Formatting;
using Nido.Application.Telegram.Messaging;
using Nido.Application.Telegram.Menu;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Messaging;
using Xunit;

namespace Nido.Api.IntegrationTests.Telegram;

[Collection("TelegramWebhook")]
public sealed class TelegramWebhookEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private const string Secret = "default-test-webhook-secret";
    private const string Endpoint = "/api/webhooks/telegram";
    private const long AcceptedUpdateId = 11_001L;
    private const long DuplicateUpdateId = 11_002L;

    private readonly NidoTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public TelegramWebhookEndpointTests(NidoTestWebAppFactory baseFactory)
    {
        _factory = baseFactory.WithTelegramWebhookConfig(Secret);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithoutSecretHeader_Returns401()
    {
        var response = await _client.PostAsync(Endpoint, BuildUpdate(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoProcessedRowAsync(1);
    }

    [Fact]
    public async Task Post_WithWrongSecret_Returns401_WithoutDeserializingBody()
    {
        using var content = new StringContent(BuildUpdateBody(2), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", "definitely-wrong");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoProcessedRowAsync(2);
    }

    [Fact]
    public async Task Post_WithCorrectSecret_AndNewUpdateId_Returns200_AndPersistsRow()
    {
        var response = await PostUpdateAsync(AcceptedUpdateId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertProcessedRowAsync(AcceptedUpdateId);
    }

    [Fact]
    public async Task Post_WithDuplicateUpdateId_Returns200_WithoutInsertingAgain()
    {
        var first = await PostUpdateAsync(DuplicateUpdateId);
        var second = await PostUpdateAsync(DuplicateUpdateId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await AssertExactlyOneRowAsync(DuplicateUpdateId);
    }

    [Theory]
    [InlineData("{\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}")]
    [InlineData("{\"update_id\":0,\"message\":{\"message_id\":1,\"date\":1,\"text\":\"hi\",\"chat\":{\"id\":1,\"type\":\"private\"}}}")]
    public async Task Post_WithFunctionallyInvalidPayload_Returns400_AndDoesNotPersist(string body)
    {
        var countBefore = await CountProcessedRowsAsync(_factory);
        var response = await PostRawBodyAsync(_client, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var countAfter = await CountProcessedRowsAsync(_factory);
        Assert.Equal(countBefore, countAfter);
        await AssertNoProcessedRowAsync(0);
    }

    [Fact]
    public async Task Post_WithMalformedJson_Returns400_WithoutWritingRow()
    {
        using var content = new StringContent("{not-json", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithOversizedBody_Returns413_BeforeRateLimitConsumption()
    {
        const int maxBytes = 8 * 1024;
        using var factory = _factory.WithTelegramWebhookConfig(Secret, maxPayloadBytes: maxBytes);
        using var client = factory.CreateClient();

        var oversized = new string('a', 70 * 1024);
        using var content = new StringContent(
            "{\"update_id\":99,\"message\":{\"text\":\"" + oversized + "\"}}",
            Encoding.UTF8,
            "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        await AssertNoProcessedRowAsync(factory, 99);
    }

    [Fact]
    public async Task Post_OverRateLimit_Returns429_AndDoesNotPersist()
    {
        using var factory = _factory.WithTelegramWebhookConfig(
            secret: Secret,
            maxPayloadBytes: 65_536,
            rateLimitPermitPerWindow: 3,
            rateLimitWindowSeconds: 60);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var ok = await PostUpdateAsync(client, 8000L + i);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var blocked = await PostUpdateAsync(client, 8003L);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.TryGetValues("Retry-After", out var retryAfterValues));
        Assert.Equal("60", Assert.Single(retryAfterValues));

        await AssertNoProcessedRowAsync(factory, 8003);
    }

    [Fact]
    public async Task Post_MenuCommand_WithLinkedMember_SendsMainMenu_AndStoresConversationState()
    {
        using var factory = CreateEnqueueOnlyFactory();

        await SeedLinkedCurrentMemberAsync(factory, 301);

        using var client = factory.CreateClient();
        var response = await PostMessageAsync(client, 12_001, 301, "/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await AssertSingleOutboxPayloadAsync(factory, 301);
        Assert.Contains(MarkdownV2Escaper.Escape(TelegramMenuCopy.MainMenuText), payload.Text, StringComparison.Ordinal);
        Assert.Contains("Ver productos por vencer", payload.Text, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var stateStore = scope.ServiceProvider.GetRequiredService<ITelegramConversationStateStore>();
        var state = await stateStore.GetAsync(301, CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(TelegramMenuCopy.MainMenuId, state!.MenuId);
    }

    [Fact]
    public async Task Post_DigitSelection_AfterMenu_RoutesToRealProvider()
    {
        using var factory = CreateEnqueueOnlyFactory();

        await SeedLinkedCurrentMemberAsync(factory, 302);

        using var client = factory.CreateClient();
        var menuResponse = await PostMessageAsync(client, 12_101, 302, "/menu");
        var digitResponse = await PostMessageAsync(client, 12_102, 302, "2");

        Assert.Equal(HttpStatusCode.OK, menuResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, digitResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var payloads = await db.TelegramOutboxMessages.AsNoTracking()
            .Where(x => x.ChatId == 302)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.PayloadJson)
            .ToListAsync();

        Assert.Equal(2, payloads.Count);
        Assert.Contains("La alacena está vacía por ahora", DeserializePayload(payloads[1]).Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_DigitSelection_WithoutState_SendsRecoveryMainMenu()
    {
        using var factory = CreateEnqueueOnlyFactory();

        await SeedLinkedCurrentMemberAsync(factory, 303);

        using var client = factory.CreateClient();
        var response = await PostMessageAsync(client, 12_201, 303, "2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await AssertSingleOutboxPayloadAsync(factory, 303);
        Assert.Contains("ya no está disponible", payload.Text, StringComparison.Ordinal);
        Assert.Contains("Ver productos por vencer", payload.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_TaskCompletion_FullFlow_AssignsCompletionAndClearsPayload()
    {
        using var factory = CreateEnqueueOnlyFactory();

        var (tareaId, _) = await SeedLinkedCurrentMemberWithPendingTaskAsync(factory, 305, "Sacar la basura");

        using var client = factory.CreateClient();
        // Step 1: open the menu (state stores main-menu with no payload).
        var menuResponse = await PostMessageAsync(client, 13_001, 305, "/menu");
        Assert.Equal(HttpStatusCode.OK, menuResponse.StatusCode);

        // Step 2: select option 4 — provider must return a numbered task
        // list and persist the tasks.complete payload to state.
        var option4Response = await PostMessageAsync(client, 13_002, 305, "4");
        Assert.Equal(HttpStatusCode.OK, option4Response.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var stateStore = scope.ServiceProvider.GetRequiredService<ITelegramConversationStateStore>();
            var state = await stateStore.GetAsync(305, CancellationToken.None);
            Assert.NotNull(state);
            Assert.NotNull(state!.PayloadJson);
            var payload = Nido.Application.Telegram.Conversation.TelegramTaskCompletionPayload.TryParse(state.PayloadJson);
            Assert.NotNull(payload);
            Assert.Single(payload!.Choices);
            Assert.Equal(tareaId, payload.Choices[0].TaskId);
        }

        // Step 3: reply with the choice. The dispatcher must call
        // CompletarTareaHandler, write the audit fields, and clear the
        // payload.
        var choiceResponse = await PostMessageAsync(client, 13_003, 305, "1");
        Assert.Equal(HttpStatusCode.OK, choiceResponse.StatusCode);

        using (var verifyScope = factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
            var tarea = await db.Tareas.AsNoTracking().SingleAsync(t => t.Id == tareaId);
            Assert.Equal("completada", tarea.Estado);
            Assert.NotNull(tarea.FechaCompletado);
            Assert.NotNull(tarea.CompletadoPor);

            var stateStore = verifyScope.ServiceProvider.GetRequiredService<ITelegramConversationStateStore>();
            var state = await stateStore.GetAsync(305, CancellationToken.None);
            Assert.NotNull(state);
            Assert.Null(state!.PayloadJson);
        }

        // Step 4: confirm the user-facing message is the success copy with
        // the dedicated message type. The outbox stores the
        // MarkdownV2-escaped text, so we assert on a prefix that survives
        // escaping intact.
        var outbox = await GetOutboxForChatAsync(factory, 305);
        var successMessage = Assert.Single(outbox, x => x.Type == TelegramMenuCopy.TaskCompletionMessageType);
        Assert.StartsWith("Listo, marqué la tarea como completada", successMessage.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_TaskCompletion_OutOfRangeReply_ReturnsRecoveryAndPreservesTask()
    {
        using var factory = CreateEnqueueOnlyFactory();

        var (tareaId, _) = await SeedLinkedCurrentMemberWithPendingTaskAsync(factory, 306, "Lavar platos");

        using var client = factory.CreateClient();
        await PostMessageAsync(client, 13_101, 306, "/menu");
        await PostMessageAsync(client, 13_102, 306, "4");
        var response = await PostMessageAsync(client, 13_103, 306, "9");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var tarea = await db.Tareas.AsNoTracking().SingleAsync(t => t.Id == tareaId);
        Assert.NotEqual("completada", tarea.Estado);

        var outbox = await GetOutboxForChatAsync(factory, 306);
        var recovery = Assert.Single(outbox, x => x.Type == TelegramMenuCopy.TaskCompletionRecoveryMessageType);
        // Assert on the unescaped prefix; the rest of the message is
        // re-rendered task copy that contains MarkdownV2-sensitive chars.
        Assert.StartsWith("Ese número no corresponde", recovery.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_MenuCommand_WhenChatNotLinked_Returns200_AndSendsPairingRecovery()
    {
        using var factory = CreateEnqueueOnlyFactory();

        using var client = factory.CreateClient();
        var response = await PostMessageAsync(client, 12_251, 399, "/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await AssertSingleOutboxPayloadAsync(factory, 399);
        Assert.Contains("no está vinculado", payload.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_MenuCommand_WhenOutboxEnqueueFails_ReleasesReservation_AndRetryCanRecoverConfirmation()
    {
        using var factory = CreateEnqueueOnlyFactory().WithStorageOverride(services =>
        {
            services.RemoveAll<ITelegramOutboxWriter>();
            services.AddScoped<ITelegramOutboxWriter, FlakyTelegramOutboxWriter>();
        });

        await SeedLinkedCurrentMemberAsync(factory, 397);

        using var client = factory.CreateClient();
        var first = await PostMessageAsync(client, 12_253, 397, "/menu");
        var retry = await PostMessageAsync(client, 12_253, 397, "/menu");

        Assert.NotEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var outboxCount = await db.TelegramOutboxMessages.AsNoTracking().CountAsync(x => x.ChatId == 397);
        var processedCount = await db.ProcessedTelegramUpdates.AsNoTracking().CountAsync(x => x.UpdateId == 12_253);

        Assert.Equal(1, outboxCount);
        Assert.Equal(1, processedCount);
    }

    [Fact]
    public async Task Post_MenuCommand_WhenMembershipIsStale_Returns200_UnlinksChat_AndSendsRecovery()
    {
        using var factory = CreateEnqueueOnlyFactory();

        await SeedLinkedCurrentMemberAsync(factory, 398);
        await RemoveMembershipAsync(factory, 398);

        using var client = factory.CreateClient();
        var response = await PostMessageAsync(client, 12_252, 398, "/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await AssertSingleOutboxPayloadAsync(factory, 398);
        Assert.Contains("ya no está disponible", payload.Text, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var link = await db.TelegramChatLinks.SingleAsync(x => x.ChatId == 398);
        Assert.NotNull(link.UnpairedAt);
    }

    [Fact]
    public async Task Post_Unlink_ClearsConversationState()
    {
        using var factory = CreateEnqueueOnlyFactory();

        await SeedLinkedCurrentMemberAsync(factory, 304);
        using (var scope = factory.Services.CreateScope())
        {
            var stateStore = scope.ServiceProvider.GetRequiredService<ITelegramConversationStateStore>();
            await stateStore.SetAsync(new TelegramConversationState(304, TelegramMenuCopy.MainMenuId, DateTime.UtcNow, null), CancellationToken.None);
        }

        using var client = factory.CreateClient();
        var response = await PostMessageAsync(client, 12_301, 304, "/unlink");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var link = await db.TelegramChatLinks.SingleAsync(x => x.ChatId == 304);
        Assert.NotNull(link.UnpairedAt);

        var stateStoreAfter = verifyScope.ServiceProvider.GetRequiredService<ITelegramConversationStateStore>();
        var state = await stateStoreAfter.GetAsync(304, CancellationToken.None);
        Assert.Null(state);
    }

    private Task<HttpResponseMessage> PostUpdateAsync(long updateId)
        => PostUpdateAsync(_client, updateId);

    private static async Task<HttpResponseMessage> PostUpdateAsync(HttpClient client, long updateId)
    {
        using var content = BuildUpdate(updateId);
        return await PostAsync(client, content);
    }

    private static async Task<HttpResponseMessage> PostMessageAsync(HttpClient client, long updateId, long chatId, string text)
    {
        using var content = BuildUpdate(updateId, chatId, text);
        return await PostAsync(client, content);
    }

    private static async Task<HttpResponseMessage> PostRawBodyAsync(HttpClient client, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await PostAsync(client, content);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", Secret);
        return await client.SendAsync(request);
    }

    private static StringContent BuildUpdate(long updateId)
        => BuildUpdate(updateId, 1, "hi");

    private static StringContent BuildUpdate(long updateId, long chatId, string text)
        => new(BuildUpdateBody(updateId, chatId, text), Encoding.UTF8, "application/json");

    private static string BuildUpdateBody(long updateId)
        => BuildUpdateBody(updateId, 1, "hi");

    private static string BuildUpdateBody(long updateId, long chatId, string text)
        => "{\"update_id\":" + updateId + ",\"message\":{\"message_id\":1,\"date\":1,\"text\":"
            + System.Text.Json.JsonSerializer.Serialize(text)
            + ",\"chat\":{\"id\":" + chatId + ",\"type\":\"private\"}}}";

    private async Task AssertNoProcessedRowAsync(long updateId)
        => await AssertNoProcessedRowAsync(_factory, updateId);

    private static async Task AssertNoProcessedRowAsync(NidoTestWebAppFactory factory, long updateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.False(exists, $"Expected no row in processed_telegram_updates for update_id={updateId}.");
    }

    private static async Task<int> CountProcessedRowsAsync(NidoTestWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        return await db.ProcessedTelegramUpdates.CountAsync();
    }

    private async Task AssertProcessedRowAsync(long updateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.ProcessedTelegramUpdates.AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId);
        Assert.True(exists, $"Expected row in processed_telegram_updates for update_id={updateId}.");
    }

    private async Task AssertExactlyOneRowAsync(long updateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var count = await db.ProcessedTelegramUpdates.AsNoTracking()
            .CountAsync(p => p.UpdateId == updateId);
        Assert.Equal(1, count);
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
            Email = $"telegram-{chatId}@example.com",
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

    private static async Task<(Guid TareaId, Guid UsuarioId)> SeedLinkedCurrentMemberWithPendingTaskAsync(NidoTestWebAppFactory factory, long chatId, string titulo)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tareaId = Guid.NewGuid();

        db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Telegram",
            Email = $"telegram-tarea-{chatId}@example.com",
            PasswordHash = "hash",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Hogares.Add(new Hogare { Id = hogarId, Nombre = $"Hogar Tarea {chatId}", CreatedAt = DateTime.UtcNow });
        db.MiembrosHogars.Add(new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            HogarId = hogarId,
            Rol = "owner",
            Puntos = 0
        });
        db.TelegramChatLinks.Add(new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UsuarioId = usuarioId,
            HogarId = hogarId,
            PairedAt = DateTime.UtcNow
        });

        var tarea = new Tarea
        {
            Id = tareaId,
            HogarId = hogarId,
            CreadoPor = usuarioId,
            CreadoPorNavigation = await db.Usuarios.FindAsync(usuarioId) ?? throw new InvalidOperationException(),
            Titulo = titulo,
            Estado = "pendiente",
            CreatedAt = DateTime.UtcNow
        };
        db.Tareas.Add(tarea);
        db.AsignacionesTareas.Add(new AsignacionesTarea
        {
            Id = Guid.NewGuid(),
            TareaId = tareaId,
            Tarea = tarea,
            UsuarioId = usuarioId,
            Usuario = await db.Usuarios.FindAsync(usuarioId) ?? throw new InvalidOperationException(),
            FechaAsignacion = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return (tareaId, usuarioId);
    }

    private static async Task<List<OutboxRow>> GetOutboxForChatAsync(NidoTestWebAppFactory factory, long chatId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var rows = await db.TelegramOutboxMessages.AsNoTracking()
            .Where(x => x.ChatId == chatId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        return rows.Select(r => new OutboxRow(r.MessageType, DeserializePayload(r.PayloadJson).Text)).ToList();
    }

    private sealed record OutboxRow(string Type, string Text);

    private static async Task RemoveMembershipAsync(NidoTestWebAppFactory factory, long chatId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

        var link = await db.TelegramChatLinks.AsNoTracking().SingleAsync(x => x.ChatId == chatId);
        var memberships = await db.MiembrosHogars
            .Where(x => x.UsuarioId == link.UsuarioId && x.HogarId == link.HogarId)
            .ToListAsync();

        db.MiembrosHogars.RemoveRange(memberships);
        await db.SaveChangesAsync();
    }

    private static async Task<TelegramOutboxPayload> AssertSingleOutboxPayloadAsync(NidoTestWebAppFactory factory, long chatId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var row = await db.TelegramOutboxMessages.AsNoTracking().SingleAsync(x => x.ChatId == chatId);
        return DeserializePayload(row.PayloadJson);
    }

    private static TelegramOutboxPayload DeserializePayload(string payloadJson)
        => System.Text.Json.JsonSerializer.Deserialize<TelegramOutboxPayload>(payloadJson)
           ?? throw new InvalidOperationException("Telegram outbox payload could not be deserialized.");

    private NidoTestWebAppFactory CreateEnqueueOnlyFactory()
        => _factory.WithStorageOverride(services =>
        {
            var senderWorkerRegistrations = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType == typeof(TelegramSenderWorker))
                .ToList();

            foreach (var descriptor in senderWorkerRegistrations)
            {
                services.Remove(descriptor);
            }
        });

    private sealed class FlakyTelegramOutboxWriter(NidoDbContext db, ITelegramOutboxWakeupService wakeupService) : ITelegramOutboxWriter
    {
        private static int _attempts;

        public Task<TelegramMessageResult> EnqueueAsync(EnqueueTelegramMessageRequest request, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                throw new InvalidOperationException("Simulated outbox failure.");
            }

            return new TelegramOutboxWriter(db, wakeupService, new TelegramOptions { BotToken = "test_token" }, NullLogger<TelegramOutboxWriter>.Instance).EnqueueAsync(request, ct);
        }
    }
}
