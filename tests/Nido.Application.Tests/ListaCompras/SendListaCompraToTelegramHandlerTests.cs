using Nido.Application.ListaCompras;
using Nido.Application.Telegram.Messaging;
using Nido.Application.Telegram.Pairing;
using Microsoft.Extensions.Options;
using TelegramConstants = Nido.Application.Telegram.TelegramConstants;
using TelegramOptions = Nido.Application.Telegram.TelegramOptions;

namespace Nido.Application.Tests.ListaCompras;

public sealed class SendListaCompraToTelegramHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasActiveLink_EnqueuesTelegramMessage()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var listaId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ActiveResult = [new ListaCompraGrupoResult("Verdulería", [new ListaCompraItemResult(Guid.NewGuid(), null, "Leche *light* [1L]", 1, "lt", false, null, 0)])],
            ScopedResult = [new ListaCompraGrupoResult("Verdulería", [new ListaCompraItemResult(Guid.NewGuid(), null, "Leche *light* [1L]", 1, "lt", false, null, 0)])]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(123_456_789, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, listaId), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.Enqueued, result.Status);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(123_456_789, result.ChatId);
        Assert.Equal(listaId, result.ListaId);
        Assert.True(batcher.LastIsCritical);
        Assert.Equal("lista_compras", batcher.LastMessageType);
        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains("Leche \\*light\\* \\[1L\\]", text);
        Assert.Equal(TelegramConstants.ParseModeMarkdownV2, payload.RootElement.GetProperty("parse_mode").GetString());
        Assert.Equal(listaId, repository.LastRequestedListaId);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoTelegramLink_ReturnsNoTelegramLinkWithoutEnqueue()
    {
        var repository = new FakeListaComprasRepository();
        var pairingRepository = new FakePairingRepository { ActiveLinkResult = null };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.NoTelegramLink, result.Status);
        Assert.Empty(batcher.EnqueuedEvents);
    }

    [Fact]
    public async Task Handle_WhenListHasNoPendingItems_ReturnsEmpty()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository();
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(55, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, null), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.Empty, result.Status);
        Assert.Empty(batcher.EnqueuedEvents);
    }

    [Fact]
    public async Task Handle_WhenListScopeProvided_UsesScopedQuery()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var listaId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ScopedResult = [new ListaCompraGrupoResult("Principal", [new ListaCompraItemResult(Guid.NewGuid(), null, "Arroz", 1, "kg", false, null, 0)])]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(77, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, listaId), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.Enqueued, result.Status);
        Assert.Equal(listaId, repository.LastRequestedListaId);
        Assert.Equal(listaId, result.ListaId);
    }

    [Fact]
    public async Task Handle_WhenViewAllRequested_ConsolidatesDuplicateItemsAcrossGroups()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ActiveResult =
            [
                new ListaCompraGrupoResult("Receta 1",
                [
                    new ListaCompraItemResult(Guid.NewGuid(), null, "Azúcar", 1.5m, "kg", false, null, 0),
                    new ListaCompraItemResult(Guid.NewGuid(), null, "Leche", 1, "lt", false, null, 0)
                ]),
                new ListaCompraGrupoResult("Receta 2",
                [
                    new ListaCompraItemResult(Guid.NewGuid(), null, "azucar", 0.5m, "kg", false, null, 0),
                    new ListaCompraItemResult(Guid.NewGuid(), null, "AZÚCAR", 2, "kg", false, null, 0)
                ])
            ]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(81, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, null), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.Enqueued, result.Status);
        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains("• *Azúcar* — 4 kg", text);
        Assert.DoesNotContain("Receta 1", text);
        Assert.DoesNotContain("Receta 2", text);
        Assert.Equal(1, CountOccurrences(text!, "• *Azúcar* — 4 kg"));
    }

    [Fact]
    public async Task Handle_WhenSpecificListRequested_DoesNotConsolidateAcrossOtherGroups()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var listaId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ActiveResult =
            [
                new ListaCompraGrupoResult("Otra lista", [new ListaCompraItemResult(Guid.NewGuid(), null, "Arroz", 4, "kg", false, null, 0)]),
                new ListaCompraGrupoResult("Otra lista 2", [new ListaCompraItemResult(Guid.NewGuid(), null, "Arroz", 3, "kg", false, null, 0)])
            ],
            ScopedResult =
            [
                new ListaCompraGrupoResult("Principal", [new ListaCompraItemResult(Guid.NewGuid(), null, "Arroz", 1, "kg", false, null, 0)])
            ],
            ListsResult = [new ListaCompraListResult(listaId, "Mi Receta", DateTime.UtcNow, null, [])]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(77, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, listaId), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.Enqueued, result.Status);
        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains("Mi Receta", text);
        Assert.Contains("Principal", text);
        Assert.Contains("• *Arroz* — 1 kg", text);
        Assert.DoesNotContain("• *Arroz* — 8 kg", text);
        Assert.DoesNotContain("Otra lista", text);
    }

    [Fact]
    public async Task Handle_WhenViewAllRequested_PreservesMarkdownEscapingAfterConsolidation()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ActiveResult =
            [
                new ListaCompraGrupoResult("Uno", [new ListaCompraItemResult(Guid.NewGuid(), null, "Leche *light* [1L]", 1, "lt", false, null, 0)]),
                new ListaCompraGrupoResult("Dos", [new ListaCompraItemResult(Guid.NewGuid(), null, "Leche *light* [1L]", 2, "lt", false, null, 0)])
            ]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(90, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, null), CancellationToken.None);

        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains("• *Leche \\*light\\* \\[1L\\]* — 3 lt", text);
    }

    [Fact]
    public async Task Handle_WhenQuantityHasDecimals_EscapesMarkdownV2DecimalSeparator()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var listaId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ScopedResult =
            [
                new ListaCompraGrupoResult("Principal", [new ListaCompraItemResult(Guid.NewGuid(), null, "Azúcar", 1.88m, "kg", false, null, 0)])
            ],
            ListsResult = [new ListaCompraListResult(listaId, "Mi Receta", DateTime.UtcNow, null, [])]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(77, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, listaId), CancellationToken.None);

        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains("• *Azúcar* — 1\\.88 kg", text);
    }

    [Fact]
    public async Task Handle_WhenTelegramLinkIsStale_UnpairsAndReturnsNoTelegramLink()
    {
        var repository = new FakeListaComprasRepository();
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = null,
            WasStaleLinkUnpaired = true
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        var result = await handler.Handle(new SendListaCompraToTelegramCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal(SendListaCompraToTelegramStatus.NoTelegramLink, result.Status);
        Assert.True(pairingRepository.WasStaleLinkUnpaired);
        Assert.Empty(batcher.EnqueuedEvents);
    }

    [Fact]
    public async Task Handle_WhenSpecificListWithDefaultGroupRequested_OmitDefaultGroupNameAndDisplayListName()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var listaId = Guid.NewGuid();
        var repository = new FakeListaComprasRepository
        {
            ScopedResult =
            [
                new ListaCompraGrupoResult("Productos agregados",
                [
                    new ListaCompraItemResult(Guid.NewGuid(), null, "Manteca", 1, "unidad", false, null, 0),
                    new ListaCompraItemResult(Guid.NewGuid(), null, "Caldo de pollo", null, null, false, null, 1)
                ])
            ],
            ListsResult = [new ListaCompraListResult(listaId, "Arroz con almendras y arvejas", DateTime.UtcNow, null, [])]
        };
        var pairingRepository = new FakePairingRepository
        {
            ActiveLinkResult = new TelegramChatLinkResult(77, usuarioId, hogarId, DateTime.UtcNow)
        };
        var batcher = new FakeTelegramNotificationBatcher();
        var handler = new SendListaCompraToTelegramHandler(repository, pairingRepository, batcher, Options.Create(new TelegramOptions()));

        await handler.Handle(new SendListaCompraToTelegramCommand(hogarId, usuarioId, listaId), CancellationToken.None);

        using var payload = System.Text.Json.JsonDocument.Parse(batcher.LastPayloadJson!);
        var text = payload.RootElement.GetProperty("text").GetString();

        // Title matches list name
        Assert.Contains("🛒 *Arroz con almendras y arvejas*", text);
        // Default group name "Productos agregados" is omitted
        Assert.DoesNotContain("Productos agregados", text);
        // Bold item with quantity/unit and conditional separator
        Assert.Contains("• *Manteca* — 1 unidad", text);
        // Bold item without quantity/unit (no separator)
        Assert.Contains("• *Caldo de pollo*", text);
        Assert.DoesNotContain("Caldo de pollo —", text);
    }

    private sealed class FakeListaComprasRepository : IListaComprasRepository
    {
        public Guid? LastRequestedListaId { get; private set; }
        public IReadOnlyList<ListaCompraGrupoResult> ActiveResult { get; set; } = Array.Empty<ListaCompraGrupoResult>();
        public IReadOnlyList<ListaCompraGrupoResult> ScopedResult { get; set; } = Array.Empty<ListaCompraGrupoResult>();
        public IReadOnlyList<ListaCompraListResult> ListsResult { get; set; } = Array.Empty<ListaCompraListResult>();

        public Task<IReadOnlyList<ListaCompraListResult>> GetListsAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(ListsResult);

        public Task<ListaCompraListResult> CreateListAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct)
            => Task.FromResult(new ListaCompraListResult(Guid.NewGuid(), nombre, DateTime.UtcNow, null, []));

        public Task<ListaCompraListResult?> UpdateListAsync(Guid hogarId, Guid listaId, string nombre, CancellationToken ct)
            => Task.FromResult<ListaCompraListResult?>(null);

        public Task<bool> DeleteListAsync(Guid hogarId, Guid listaId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<ListaCompraItemResult?> AddItemToListAsync(Guid hogarId, Guid listaId, Guid usuarioId, string nombre, decimal? cantidad, string? unidad, CancellationToken ct)
            => Task.FromResult<ListaCompraItemResult?>(null);

        public Task<ListaCompraItemResult?> UpdateItemAsync(Guid hogarId, Guid listaId, Guid itemId, string? nombre, decimal? cantidad, string? unidad, bool? comprado, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<ListaCompraItemResult?>(null);

        public Task<bool> RemoveItemAsync(Guid id, Guid hogarId, Guid listaId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(ActiveResult);

        public Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveByListAsync(Guid hogarId, Guid listaId, CancellationToken ct)
        {
            LastRequestedListaId = listaId;
            return Task.FromResult(ScopedResult);
        }

        public Task<IReadOnlyList<ListaCompraHistorialItemResult>> GetHistorialAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraHistorialItemResult>>(Array.Empty<ListaCompraHistorialItemResult>());

        public Task<IReadOnlyList<ListaCompraGrupoResult>> ReplaceGroupAsync(Guid hogarId, Guid usuarioId, string grupoNombre, IReadOnlyList<ListaCompraItemInput> items, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraGrupoResult>>(Array.Empty<ListaCompraGrupoResult>());

        public Task<ListaCompraItemResult> AddItemAsync(Guid hogarId, Guid usuarioId, string nombre, decimal? cantidad, string? unidad, string grupoNombre, CancellationToken ct)
            => Task.FromResult(new ListaCompraItemResult(Guid.NewGuid(), null, nombre, cantidad, unidad, false, null, 0));

        public Task<ListaCompraItemResult?> MarkPurchasedAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<ListaCompraItemResult?>(null);

        public Task<IReadOnlyList<ListaCompraItemResult>> MarkPurchasedByNameAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraItemResult>>(Array.Empty<ListaCompraItemResult>());

        public Task<bool> MarkAddedToInventoryAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> RemoveItemAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);

        public Task ClearActiveAsync(Guid hogarId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakePairingRepository : ITelegramPairingRepository
    {
        public TelegramChatLinkResult? ActiveLinkResult { get; set; }
        public bool WasStaleLinkUnpaired { get; set; }

        public Task<TelegramPairingTokenResult> CreatePairingTokenAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(Guid hogarId, Guid usuarioId, string tokenHash, DateTime tokenExpiresAt, string codeHash, DateTime codeExpiresAt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(string codeHash, long chatId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<UnlinkTelegramChatResult> UnlinkActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TelegramChatLinkResult?> GetActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(ActiveLinkResult);

        public Task<TelegramChatLinkResult?> GetActiveLinkForCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        {
            if (WasStaleLinkUnpaired)
            {
                ActiveLinkResult = null;
            }

            return Task.FromResult(ActiveLinkResult);
        }
    }

    private sealed class FakeTelegramNotificationBatcher : ITelegramNotificationBatcher
    {
        public List<(Guid HogarId, long ChatId, string MessageType, string PayloadJson, bool IsCritical)> EnqueuedEvents { get; } = [];
        public string? LastMessageType { get; private set; }
        public string? LastPayloadJson { get; private set; }
        public bool LastIsCritical { get; private set; }

        public Task EnqueueEventAsync(Guid hogarId, long chatId, string messageType, string payloadJson, bool isCritical, CancellationToken ct = default)
        {
            EnqueuedEvents.Add((hogarId, chatId, messageType, payloadJson, isCritical));
            LastMessageType = messageType;
            LastPayloadJson = payloadJson;
            LastIsCritical = isCritical;
            return Task.CompletedTask;
        }

        public Task ProcessExpiredBatchesAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
