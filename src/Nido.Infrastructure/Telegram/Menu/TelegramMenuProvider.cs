using System.Globalization;
using Microsoft.Extensions.Configuration;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Menu;

namespace Nido.Infrastructure.Telegram.Menu;

public sealed class TelegramMenuProvider(
    ITelegramMenuReadService readService,
    IConfiguration? configuration = null) : ITelegramMenuProvider
{
    private const int ExpiringSoonDays = 7;
    private const int MaxExpiringItems = 8;
    private const int MaxCategoryLines = 4;
    private const int MaxProductLines = 5;
    private const int MaxShoppingItems = 12;
    private const int MaxTaskItems = 8;

    public Task<TelegramMenuRenderResult> RenderMenuAsync(TelegramMenu menu, TelegramChatLinkSnapshot link, CancellationToken ct)
        => Task.FromResult(new TelegramMenuRenderResult(TelegramMenuCopy.MainMenuText));

    public async Task<TelegramMenuSelectionResult> SelectAsync(string menuId, string optionKey, TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        if (optionKey == "4")
        {
            return await BuildPendingTasksSelectionAsync(link, ct);
        }

        var text = optionKey switch
        {
            "1" => await BuildExpiringProductsTextAsync(link, ct),
            "2" => await BuildPantrySummaryTextAsync(link, ct),
            "3" => await BuildShoppingListTextAsync(link, ct),
            "5" => BuildOpenNidoText(),
            _ => null
        };

        return new TelegramMenuSelectionResult(
            text is not null,
            text ?? string.Empty,
            text is null ? null : menuId,
            false);
    }

    private async Task<string> BuildExpiringProductsTextAsync(TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await readService.GetExpiringStockAsync(link.HogarId, today, ExpiringSoonDays, MaxExpiringItems, ct);

        if (result.Items.Count == 0)
        {
            return $"No hay productos por vencer en los próximos {ExpiringSoonDays} días.";
        }

        var lines = result.Items.Select(item =>
            $"• {item.ProductName} — {BuildQuantityText(item.Quantity, item.Unit, item.ContainerCount)} · vence {FormatDueLabel(item.DueDate, today)}");

        var messageLines = new List<string> { $"Productos por vencer ({ExpiringSoonDays} días)" };
        messageLines.AddRange(lines);
        var message = string.Join('\n', messageLines);

        if (result.HasMore)
        {
            message += $"\n…y {result.RemainingCount} más.";
        }

        return message;
    }

    private async Task<string> BuildPantrySummaryTextAsync(TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        var summary = await readService.GetPantrySummaryAsync(link.HogarId, MaxCategoryLines, MaxProductLines, ct);

        if (summary.TotalUnits == 0)
        {
            return "La alacena está vacía por ahora.";
        }

        var categories = summary.TopCategories
            .Select(line => $"• {line.Name}: {line.Count}");

        var products = summary.TopProducts
            .Select(line => $"• {line.Name}: {line.Count}");

        var summaryLines = new List<string>
        {
            "Resumen de alacena",
            $"• {summary.TotalUnits} {Pluralize(summary.TotalUnits, "unidad", "unidades")} en stock",
            $"• {summary.DistinctProductCount} {Pluralize(summary.DistinctProductCount, "producto distinto", "productos distintos")}",
            $"• {summary.DistinctCategoryCount} {Pluralize(summary.DistinctCategoryCount, "categoría", "categorías")}",
            "Categorías:"
        };
        summaryLines.AddRange(categories);
        summaryLines.Add("Productos:");
        summaryLines.AddRange(products);
        return string.Join('\n', summaryLines);
    }

    private async Task<string> BuildShoppingListTextAsync(TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        var result = await readService.GetPendingShoppingItemsAsync(link.HogarId, MaxShoppingItems, ct);

        if (result.Items.Count == 0)
        {
            return "La lista de compras está al día.";
        }

        var lines = new List<string> { "Lista de compras pendiente" };
        string? currentGroup = null;

        foreach (var item in result.Items)
        {
            if (!string.Equals(currentGroup, item.GroupName, StringComparison.Ordinal))
            {
                currentGroup = item.GroupName;
                if (item.GroupName is not null)
                {
                    lines.Add($"{item.GroupName}:");
                }
            }

            var quantityText = BuildQuantityText(item.Quantity, item.Unit, item.ContainerCount);
            lines.Add(quantityText.Length == 0 ? $"• {item.Name}" : $"• {item.Name} — {quantityText}");
        }

        if (result.HasMore)
        {
            lines.Add($"…y {result.RemainingCount} más.");
        }

        return string.Join('\n', lines);
    }

    private async Task<TelegramMenuSelectionResult> BuildPendingTasksSelectionAsync(TelegramChatLinkSnapshot link, CancellationToken ct)
    {
        var result = await readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, MaxTaskItems, ct);

        if (result.Items.Count == 0)
        {
            return new TelegramMenuSelectionResult(
                true,
                TelegramMenuCopy.TaskCompletionEmptyListText,
                TelegramMenuCopy.MainMenuId,
                false,
                PayloadJson: null);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var choices = new List<TelegramTaskCompletionChoice>(result.Items.Count);
        var lines = new List<string>(result.Items.Count + 3)
        {
            TelegramMenuCopy.TaskCompletionHeaderText,
            string.Empty,
            TelegramMenuCopy.TaskCompletionBackOptionText
        };

        for (var i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            var index = i + 1;
            choices.Add(new TelegramTaskCompletionChoice(index, item.TaskId));
            lines.Add(BuildNumberedTaskLine(index, item, today));
        }

        var message = string.Join('\n', lines);
        if (result.HasMore)
        {
            message += $"\n…y {result.RemainingCount} más.";
        }

        message += $"\n\n{TelegramMenuCopy.TasksCompletionPrompt}";

        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            choices);

        return new TelegramMenuSelectionResult(
            true,
            message,
            TelegramMenuCopy.MainMenuId,
            false,
            payload.Serialize());
    }

    private static string BuildNumberedTaskLine(int index, TelegramPendingTaskItem item, DateOnly today)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.Status) && !string.Equals(item.Status, "pendiente", StringComparison.OrdinalIgnoreCase))
        {
            details.Add(item.Status.Trim());
        }

        if (item.DueDate.HasValue)
        {
            details.Add($"vence {FormatDueLabel(item.DueDate.Value, today)}");
        }

        return details.Count == 0
            ? $"{index}. {item.Title}"
            : $"{index}. {item.Title} — {string.Join(" · ", details)}";
    }

    private string BuildOpenNidoText()
    {
        var frontendBaseUrl = configuration?["Frontend:BaseUrl"]?.Trim();
        return string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? "Abrí la app de Nido para seguir desde ahí."
            : $"Abrí Nido desde acá: {frontendBaseUrl.TrimEnd('/')}";
    }

    private static string BuildQuantityText(decimal? quantity, string? unit, int envases)
    {
        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        var prefix = envases > 1 ? $"{envases} x " : string.Empty;

        if (!quantity.HasValue)
        {
            return envases > 1
                ? $"{envases} {Pluralize(envases, "envase", "envases")}"
                : string.Empty;
        }

        var quantityText = quantity.Value.ToString("0.##", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(normalizedUnit)
            ? $"{prefix}{quantityText}"
            : $"{prefix}{quantityText} {normalizedUnit}";
    }

    private static string FormatDueLabel(DateOnly dueDate, DateOnly today)
    {
        var days = dueDate.DayNumber - today.DayNumber;
        return days switch
        {
            0 => "hoy",
            1 => "mañana",
            _ => dueDate.ToString("dd/MM", CultureInfo.InvariantCulture)
        };
    }

    private static string Pluralize(int count, string singular, string plural)
        => count == 1 ? singular : plural;
}
