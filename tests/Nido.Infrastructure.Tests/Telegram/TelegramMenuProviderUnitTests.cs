using Microsoft.Extensions.Configuration;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Menu;
using Nido.Infrastructure.Telegram.Menu;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramMenuProviderUnitTests
{
    [Fact]
    public async Task RenderMenuAsync_AlwaysReturnsMainMenuCopy()
    {
        var provider = BuildProvider(new FakeTelegramMenuReadService());

        var result = await provider.RenderMenuAsync(
            new TelegramMenu("main-menu", Array.Empty<TelegramMenuOption>()),
            Link(),
            CancellationToken.None);

        Assert.Equal(TelegramMenuCopy.MainMenuText, result.Text);
    }

    [Theory]
    [InlineData("1", "No hay productos por vencer")]
    [InlineData("2", "La alacena está vacía")]
    [InlineData("3", "La lista de compras está al día")]
    [InlineData("4", "No tenés tareas pendientes asignadas")]
    public async Task SelectAsync_WhenReadServiceReturnsEmpty_ForwardsEmptyStateCopy(string optionKey, string expectedText)
    {
        var provider = BuildProvider(new FakeTelegramMenuReadService());

        var result = await provider.SelectAsync("main-menu", optionKey, Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains(expectedText, result.Text, StringComparison.Ordinal);
        Assert.False(result.ShouldClearState);
        Assert.Equal("main-menu", result.NextMenuId);
    }

    [Fact]
    public async Task SelectAsync_Option1_DelegatesExpiringStockReadAndRendersProducts()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var readService = new FakeTelegramMenuReadService
        {
            ExpiringStock = new TelegramExpiringStockReadResult(
                new[]
                {
                    new TelegramExpiringStockItem("Yogur", 2m, "u", 1, today.AddDays(1)),
                    new TelegramExpiringStockItem("Leche", 1m, "lt", 1, today.AddDays(3))
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "1", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains("Productos por vencer", result.Text, StringComparison.Ordinal);
        Assert.Contains("Yogur", result.Text, StringComparison.Ordinal);
        Assert.Contains("vence mañana", result.Text, StringComparison.Ordinal);
        Assert.Contains("Leche", result.Text, StringComparison.Ordinal);
        Assert.Equal(1, readService.ExpiringStockCalls);
    }

    [Fact]
    public async Task SelectAsync_Option1_WhenOverflow_AppendsMoreIndicator()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var readService = new FakeTelegramMenuReadService
        {
            ExpiringStock = new TelegramExpiringStockReadResult(
                new[]
                {
                    new TelegramExpiringStockItem("Yogur", 2m, "u", 1, today.AddDays(1))
                },
                HasMore: true,
                RemainingCount: 2)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "1", Link(), CancellationToken.None);

        Assert.Contains("y 2 más", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectAsync_Option2_DelegatesPantryReadAndRendersSummary()
    {
        var readService = new FakeTelegramMenuReadService
        {
            Pantry = new TelegramPantrySummary(
                TotalUnits: 3,
                DistinctProductCount: 2,
                DistinctCategoryCount: 2,
                TopCategories: new[]
                {
                    new TelegramPantryLineCount("Lácteos", 2),
                    new TelegramPantryLineCount("Despensa", 1)
                },
                TopProducts: new[]
                {
                    new TelegramPantryLineCount("Leche", 2),
                    new TelegramPantryLineCount("Arroz", 1)
                })
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "2", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains("Resumen de alacena", result.Text, StringComparison.Ordinal);
        Assert.Contains("3 unidades en stock", result.Text, StringComparison.Ordinal);
        Assert.Contains("2 productos distintos", result.Text, StringComparison.Ordinal);
        Assert.Contains("Lácteos: 2", result.Text, StringComparison.Ordinal);
        Assert.Contains("Despensa: 1", result.Text, StringComparison.Ordinal);
        Assert.Contains("Leche: 2", result.Text, StringComparison.Ordinal);
        Assert.Equal(1, readService.PantryCalls);
    }

    [Fact]
    public async Task SelectAsync_Option3_DelegatesShoppingReadAndRendersPendingItems()
    {
        var readService = new FakeTelegramMenuReadService
        {
            Shopping = new TelegramShoppingReadResult(
                new[]
                {
                    new TelegramShoppingItem("Arroz", 1m, "kg", 1, "Cena"),
                    new TelegramShoppingItem("Bananas", 6m, "u", 1, "Frutas")
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "3", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains("Lista de compras pendiente", result.Text, StringComparison.Ordinal);
        Assert.Contains("Cena:", result.Text, StringComparison.Ordinal);
        Assert.Contains("Frutas:", result.Text, StringComparison.Ordinal);
        Assert.Contains("Arroz — 1 kg", result.Text, StringComparison.Ordinal);
        Assert.Contains("Bananas — 6 u", result.Text, StringComparison.Ordinal);
        Assert.Equal(1, readService.ShoppingCalls);
    }

    [Fact]
    public async Task SelectAsync_Option3_NullGroupNameDoesNotEmitGroupHeader()
    {
        // The read service is responsible for collapsing the manual group
        // ("Productos agregados") to null. The provider's only contract is
        // "null group name => no header", which we exercise here.
        var readService = new FakeTelegramMenuReadService
        {
            Shopping = new TelegramShoppingReadResult(
                new[]
                {
                    new TelegramShoppingItem("Pan", null, null, 1, null)
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "3", Link(), CancellationToken.None);

        Assert.Contains("• Pan", result.Text, StringComparison.Ordinal);
        var lines = result.Text.Split('\n');
        var titleIndex = Array.FindIndex(lines, line => line == "Lista de compras pendiente");
        Assert.True(titleIndex >= 0);
        var nextLine = lines[titleIndex + 1];
        Assert.StartsWith("•", nextLine);
        Assert.DoesNotContain(":", nextLine.Split("•")[1]);
    }

    [Fact]
    public async Task SelectAsync_Option3_WhenOverflow_AppendsMoreIndicator()
    {
        var readService = new FakeTelegramMenuReadService
        {
            Shopping = new TelegramShoppingReadResult(
                new[]
                {
                    new TelegramShoppingItem("Arroz", 1m, "kg", 1, "General")
                },
                HasMore: true,
                RemainingCount: 4)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "3", Link(), CancellationToken.None);

        Assert.Contains("y 4 más", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectAsync_Option4_DelegatesTasksReadAndRendersAssignedTasks()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var readService = new FakeTelegramMenuReadService
        {
            Tasks = new TelegramPendingTasksReadResult(
                new[]
                {
                    new TelegramPendingTaskItem("Sacar la basura", "pendiente", today.AddDays(1), firstId),
                    new TelegramPendingTaskItem("Limpiar cocina", null, null, secondId)
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "4", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains(TelegramMenuCopy.TaskCompletionHeaderText, result.Text, StringComparison.Ordinal);
        Assert.Contains(TelegramMenuCopy.TaskCompletionBackOptionText, result.Text, StringComparison.Ordinal);
        Assert.Contains("Sacar la basura", result.Text, StringComparison.Ordinal);
        Assert.Contains("vence mañana", result.Text, StringComparison.Ordinal);
        Assert.Contains("Limpiar cocina", result.Text, StringComparison.Ordinal);
        Assert.Contains($"\n\n{TelegramMenuCopy.TaskCompletionBackOptionText}\n", result.Text, StringComparison.Ordinal);
        Assert.EndsWith(TelegramMenuCopy.TasksCompletionPrompt, result.Text, StringComparison.Ordinal);
        Assert.Equal(1, readService.TasksCalls);
    }

    [Fact]
    public async Task SelectAsync_Option4_WhenOverflow_AppendsMoreIndicator()
    {
        var readService = new FakeTelegramMenuReadService
        {
            Tasks = new TelegramPendingTasksReadResult(
                new[]
                {
                    new TelegramPendingTaskItem("Sacar la basura", null, null, Guid.NewGuid())
                },
                HasMore: true,
                RemainingCount: 1)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "4", Link(), CancellationToken.None);

        Assert.Contains("y 1 más", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectAsync_Option4_RendersTasksWithNumericPrefix()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var readService = new FakeTelegramMenuReadService
        {
            Tasks = new TelegramPendingTasksReadResult(
                new[]
                {
                    new TelegramPendingTaskItem("Sacar la basura", "pendiente", today.AddDays(1), firstId),
                    new TelegramPendingTaskItem("Limpiar cocina", null, null, secondId)
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "4", Link(), CancellationToken.None);

        Assert.Contains(TelegramMenuCopy.TaskCompletionBackOptionText, result.Text, StringComparison.Ordinal);
        Assert.Contains("1. Sacar la basura", result.Text, StringComparison.Ordinal);
        Assert.Contains("2. Limpiar cocina", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("•", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectAsync_Option4_ReturnsTaskCompletionPayload_MappingChoicesToTaskIds()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var readService = new FakeTelegramMenuReadService
        {
            Tasks = new TelegramPendingTasksReadResult(
                new[]
                {
                    new TelegramPendingTaskItem("Sacar la basura", null, null, firstId),
                    new TelegramPendingTaskItem("Limpiar cocina", null, null, secondId)
                },
                HasMore: false,
                RemainingCount: 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "4", Link(), CancellationToken.None);

        var payload = Nido.Application.Telegram.Conversation.TelegramTaskCompletionPayload.TryParse(result.PayloadJson);
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Choices.Count);
        Assert.Equal(1, payload.Choices[0].Index);
        Assert.Equal(firstId, payload.Choices[0].TaskId);
        Assert.Equal(2, payload.Choices[1].Index);
        Assert.Equal(secondId, payload.Choices[1].TaskId);
    }

    [Fact]
    public async Task SelectAsync_Option4_WhenEmpty_ReturnsEmptyMessage_AndNoPayload()
    {
        var readService = new FakeTelegramMenuReadService
        {
            Tasks = new TelegramPendingTasksReadResult(Array.Empty<TelegramPendingTaskItem>(), false, 0)
        };
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "4", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(TelegramMenuCopy.TaskCompletionEmptyListText, result.Text);
        Assert.Null(result.PayloadJson);
    }

    [Fact]
    public async Task SelectAsync_NonTaskOptions_DoNotSetPayloadJson()
    {
        var readService = new FakeTelegramMenuReadService();
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "1", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Null(result.PayloadJson);
    }

    [Fact]
    public async Task SelectAsync_Option5_DoesNotCallReadServiceAndReturnsExpectedCopy()
    {
        var readService = new FakeTelegramMenuReadService();
        var provider = BuildProvider(readService);

        var result = await provider.SelectAsync("main-menu", "5", Link(), CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains("https://app.nido.test", result.Text, StringComparison.Ordinal);
        Assert.Equal(0, readService.ExpiringStockCalls);
        Assert.Equal(0, readService.PantryCalls);
        Assert.Equal(0, readService.ShoppingCalls);
        Assert.Equal(0, readService.TasksCalls);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task SelectAsync_ForUnknownOption_ReturnsUnhandledResult(string optionKey)
    {
        var provider = BuildProvider(new FakeTelegramMenuReadService());

        var result = await provider.SelectAsync("main-menu", optionKey, Link(), CancellationToken.None);

        Assert.False(result.Handled);
        Assert.Equal(string.Empty, result.Text);
        Assert.Null(result.NextMenuId);
        Assert.False(result.ShouldClearState);
    }

    private static TelegramMenuProvider BuildProvider(ITelegramMenuReadService readService)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://app.nido.test"
            })
            .Build();

        return new TelegramMenuProvider(readService, configuration);
    }

    private static TelegramChatLinkSnapshot Link() => new(10, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null);

    private sealed class FakeTelegramMenuReadService : ITelegramMenuReadService
    {
        public TelegramExpiringStockReadResult ExpiringStock { get; set; } = new(Array.Empty<TelegramExpiringStockItem>(), false, 0);
        public TelegramPantrySummary Pantry { get; set; } = new(0, 0, 0, Array.Empty<TelegramPantryLineCount>(), Array.Empty<TelegramPantryLineCount>());
        public TelegramShoppingReadResult Shopping { get; set; } = new(Array.Empty<TelegramShoppingItem>(), false, 0);
        public TelegramPendingTasksReadResult Tasks { get; set; } = new(Array.Empty<TelegramPendingTaskItem>(), false, 0);

        public int ExpiringStockCalls { get; private set; }
        public int PantryCalls { get; private set; }
        public int ShoppingCalls { get; private set; }
        public int TasksCalls { get; private set; }

        public Task<TelegramExpiringStockReadResult> GetExpiringStockAsync(Guid hogarId, DateOnly today, int days, int limit, CancellationToken ct)
        {
            ExpiringStockCalls++;
            return Task.FromResult(ExpiringStock);
        }

        public Task<TelegramPantrySummary> GetPantrySummaryAsync(Guid hogarId, int categoryLimit, int productLimit, CancellationToken ct)
        {
            PantryCalls++;
            return Task.FromResult(Pantry);
        }

        public Task<TelegramShoppingReadResult> GetPendingShoppingItemsAsync(Guid hogarId, int limit, CancellationToken ct)
        {
            ShoppingCalls++;
            return Task.FromResult(Shopping);
        }

        public Task<TelegramPendingTasksReadResult> GetPendingAssignedTasksAsync(Guid hogarId, Guid usuarioId, int limit, CancellationToken ct)
        {
            TasksCalls++;
            return Task.FromResult(Tasks);
        }
    }
}
