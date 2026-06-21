using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Formatting;
using Nido.Application.Telegram.Messaging;
using Nido.Application.Telegram.Pairing;

namespace Nido.Application.ListaCompras;

public sealed class GetListaComprasHandler
{
    private readonly IListaComprasRepository _repository;

    public GetListaComprasHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ListaCompraGrupoResult>> Handle(Guid hogarId, CancellationToken ct)
        => _repository.GetActiveAsync(hogarId, ct);
}

public sealed class GetListasCompraHandler
{
    private readonly IListaComprasRepository _repository;

    public GetListasCompraHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ListaCompraListResult>> Handle(Guid hogarId, CancellationToken ct)
        => _repository.GetListsAsync(hogarId, ct);
}

public sealed class CreateListaCompraHandler
{
    private readonly IListaComprasRepository _repository;

    public CreateListaCompraHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraListResult> Handle(CreateListaCompraCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre de la lista es obligatorio.", nameof(command.Nombre));
        }

        return _repository.CreateListAsync(command.HogarId, command.UsuarioId, command.Nombre.Trim(), ct);
    }
}

public sealed class UpdateListaCompraHandler
{
    private readonly IListaComprasRepository _repository;

    public UpdateListaCompraHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraListResult?> Handle(UpdateListaCompraCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre de la lista es obligatorio.", nameof(command.Nombre));
        }

        return _repository.UpdateListAsync(command.HogarId, command.ListaId, command.Nombre.Trim(), ct);
    }
}

public sealed class DeleteListaCompraHandler
{
    private readonly IListaComprasRepository _repository;

    public DeleteListaCompraHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(DeleteListaCompraCommand command, CancellationToken ct)
        => _repository.DeleteListAsync(command.HogarId, command.ListaId, ct);
}

public sealed class AddListaCompraNamedItemHandler
{
    private readonly IListaComprasRepository _repository;

    public AddListaCompraNamedItemHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraItemResult?> Handle(AddListaCompraNamedItemCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(command.Nombre));
        }

        var unidad = string.IsNullOrWhiteSpace(command.Unidad) ? null : command.Unidad.Trim();
        return _repository.AddItemToListAsync(
            command.HogarId,
            command.ListaId,
            command.UsuarioId,
            command.Nombre.Trim(),
            command.Cantidad,
            unidad,
            ct);
    }
}

public sealed class UpdateListaCompraItemHandler
{
    private readonly IListaComprasRepository _repository;

    public UpdateListaCompraItemHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraItemResult?> Handle(UpdateListaCompraItemCommand command, CancellationToken ct)
    {
        var nombre = string.IsNullOrWhiteSpace(command.Nombre) ? null : command.Nombre.Trim();
        var unidad = string.IsNullOrWhiteSpace(command.Unidad) ? null : command.Unidad.Trim();
        return _repository.UpdateItemAsync(
            command.HogarId,
            command.ListaId,
            command.ItemId,
            nombre,
            command.Cantidad,
            unidad,
            command.Comprado,
            command.UsuarioId,
            ct);
    }
}

public sealed class RemoveListaCompraNamedItemHandler
{
    private readonly IListaComprasRepository _repository;

    public RemoveListaCompraNamedItemHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(RemoveListaCompraNamedItemCommand command, CancellationToken ct)
        => _repository.RemoveItemAsync(command.Id, command.HogarId, command.ListaId, ct);
}

public sealed class GetListaComprasHistorialHandler
{
    private readonly IListaComprasRepository _repository;

    public GetListaComprasHistorialHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ListaCompraHistorialItemResult>> Handle(Guid hogarId, CancellationToken ct)
        => _repository.GetHistorialAsync(hogarId, ct);
}

public sealed class AddListaCompraGroupHandler
{
    private readonly IListaComprasRepository _repository;

    public AddListaCompraGroupHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ListaCompraGrupoResult>> Handle(AddListaCompraGroupCommand command, CancellationToken ct)
    {
        var grupoNombre = NormalizeGroup(command.GrupoNombre);
        var items = command.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .Select(item => item with { Nombre = item.Nombre.Trim(), Unidad = NormalizeOptional(item.Unidad) })
            .ToList();

        return _repository.ReplaceGroupAsync(command.HogarId, command.UsuarioId, grupoNombre, items, ct);
    }

    private static string NormalizeGroup(string value)
        => string.IsNullOrWhiteSpace(value) ? ListaComprasDefaults.ManualGroupName : value.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AddListaCompraItemHandler
{
    private readonly IListaComprasRepository _repository;

    public AddListaCompraItemHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraItemResult> Handle(AddListaCompraItemCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(command.Nombre));
        }

        var grupoNombre = string.IsNullOrWhiteSpace(command.GrupoNombre)
            ? ListaComprasDefaults.ManualGroupName
            : command.GrupoNombre.Trim();

        var unidad = string.IsNullOrWhiteSpace(command.Unidad) ? null : command.Unidad.Trim();

        return _repository.AddItemAsync(
            command.HogarId,
            command.UsuarioId,
            command.Nombre.Trim(),
            command.Cantidad,
            unidad,
            grupoNombre,
            ct);
    }
}

public sealed class MarkListaCompraItemCompradoHandler
{
    private readonly IListaComprasRepository _repository;

    public MarkListaCompraItemCompradoHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<ListaCompraItemResult?> Handle(MarkListaCompraItemCompradoCommand command, CancellationToken ct)
        => _repository.MarkPurchasedAsync(command.Id, command.HogarId, command.UsuarioId, ct);
}

public sealed class MarkListaCompraItemCompradoByNameHandler
{
    private readonly IListaComprasRepository _repository;

    public MarkListaCompraItemCompradoByNameHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ListaCompraItemResult>> Handle(MarkListaCompraItemCompradoByNameCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            return Task.FromResult<IReadOnlyList<ListaCompraItemResult>>(Array.Empty<ListaCompraItemResult>());
        }

        return _repository.MarkPurchasedByNameAsync(command.HogarId, command.UsuarioId, command.Nombre.Trim(), ct);
    }
}

public sealed class RemoveListaCompraItemHandler
{
    private readonly IListaComprasRepository _repository;

    public RemoveListaCompraItemHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(RemoveListaCompraItemCommand command, CancellationToken ct)
        => _repository.RemoveItemAsync(command.Id, command.HogarId, ct);
}

public sealed class MarkListaCompraItemAgregadoInventarioHandler
{
    private readonly IListaComprasRepository _repository;

    public MarkListaCompraItemAgregadoInventarioHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(MarkListaCompraItemAgregadoInventarioCommand command, CancellationToken ct)
        => _repository.MarkAddedToInventoryAsync(command.Id, command.HogarId, ct);
}

public sealed class ClearListaComprasHandler
{
    private readonly IListaComprasRepository _repository;

    public ClearListaComprasHandler(IListaComprasRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(ClearListaComprasCommand command, CancellationToken ct)
        => _repository.ClearActiveAsync(command.HogarId, ct);
}

public sealed class SendListaCompraToTelegramHandler
{
    private sealed record ConsolidatedItem(string Nombre, decimal? Cantidad, string? Unidad);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly IListaComprasRepository _repository;
    private readonly ITelegramPairingRepository _pairingRepository;
    private readonly ITelegramNotificationBatcher _batcher;
    private readonly TelegramOptions _telegramOptions;

    public SendListaCompraToTelegramHandler(
        IListaComprasRepository repository,
        ITelegramPairingRepository pairingRepository,
        ITelegramNotificationBatcher batcher,
        IOptions<TelegramOptions> telegramOptions)
    {
        _repository = repository;
        _pairingRepository = pairingRepository;
        _batcher = batcher;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task<SendListaCompraToTelegramResult> Handle(SendListaCompraToTelegramCommand command, CancellationToken ct)
    {
        var link = await _pairingRepository.GetActiveLinkForCurrentMemberAsync(command.UsuarioId, command.HogarId, ct);
        if (link is null)
        {
            return new SendListaCompraToTelegramResult(SendListaCompraToTelegramStatus.NoTelegramLink, 0, null, command.ListaId);
        }

        var groups = command.ListaId.HasValue
            ? await _repository.GetActiveByListAsync(command.HogarId, command.ListaId.Value, ct)
            : await _repository.GetActiveAsync(command.HogarId, ct);

        var itemCount = groups.Sum(group => group.Items.Count);
        if (itemCount == 0)
        {
            return new SendListaCompraToTelegramResult(SendListaCompraToTelegramStatus.Empty, 0, link.ChatId, command.ListaId);
        }

        string? listName = null;
        if (command.ListaId.HasValue)
        {
            var lists = await _repository.GetListsAsync(command.HogarId, ct);
            listName = lists.FirstOrDefault(l => l.Id == command.ListaId.Value)?.Nombre;
        }

        var payloadJson = JsonSerializer.Serialize(new TelegramOutboxPayload(
            BuildMessage(groups, listName, consolidateItems: !command.ListaId.HasValue),
            _telegramOptions.DefaultParseMode), JsonOptions);

        await _batcher.EnqueueEventAsync(
            command.HogarId,
            link.ChatId,
            "lista_compras",
            payloadJson,
            isCritical: true,
            ct);

        return new SendListaCompraToTelegramResult(SendListaCompraToTelegramStatus.Enqueued, itemCount, link.ChatId, command.ListaId);
    }

    private static string BuildMessage(IReadOnlyList<ListaCompraGrupoResult> groups, string? listName, bool consolidateItems)
    {
        var builder = new StringBuilder();
        
        if (!string.IsNullOrWhiteSpace(listName))
        {
            builder.AppendLine($"🛒 *{MarkdownV2Escaper.Escape(listName)}*");
        }
        else
        {
            builder.AppendLine("🛒 *Lista de compras*");
        }

        if (consolidateItems)
        {
            var consolidatedItems = ConsolidateItems(groups);
            if (consolidatedItems.Count == 0)
            {
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine();

            foreach (var item in consolidatedItems)
            {
                builder.Append("• ");
                builder.Append($"*{MarkdownV2Escaper.Escape(item.Nombre)}*");

                var quantity = FormatQuantity(item.Cantidad, item.Unidad);
                if (!string.IsNullOrWhiteSpace(quantity))
                {
                    builder.Append(" — ");
                    builder.Append(quantity);
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        foreach (var group in groups)
        {
            if (group.Items.Count == 0)
            {
                continue;
            }

            builder.AppendLine();

            if (string.IsNullOrWhiteSpace(listName) || group.GrupoNombre != ListaComprasDefaults.ManualGroupName)
            {
                builder.AppendLine(MarkdownV2Escaper.Escape(group.GrupoNombre));
            }

            foreach (var item in group.Items)
            {
                builder.Append("• ");
                builder.Append($"*{MarkdownV2Escaper.Escape(item.Nombre)}*");

                var quantity = FormatQuantity(item.Cantidad, item.Unidad);
                if (!string.IsNullOrWhiteSpace(quantity))
                {
                    builder.Append(" — ");
                    builder.Append(quantity);
                }

                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static IReadOnlyList<ConsolidatedItem> ConsolidateItems(IReadOnlyList<ListaCompraGrupoResult> groups)
    {
        var consolidated = new List<ConsolidatedItem>();
        var indexesByKey = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in groups.SelectMany(group => group.Items))
        {
            var key = BuildConsolidationKey(item.Nombre, item.Unidad);
            if (indexesByKey.TryGetValue(key, out var index))
            {
                var existing = consolidated[index];
                consolidated[index] = existing with
                {
                    Cantidad = SumQuantities(existing.Cantidad, item.Cantidad)
                };

                continue;
            }

            indexesByKey[key] = consolidated.Count;
            consolidated.Add(new ConsolidatedItem(item.Nombre.Trim(), item.Cantidad, item.Unidad?.Trim()));
        }

        return consolidated;
    }

    private static string BuildConsolidationKey(string nombre, string? unidad)
        => $"{NormalizeKeyPart(nombre)}|{NormalizeKeyPart(unidad)}";

    private static string NormalizeKeyPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static decimal? SumQuantities(decimal? left, decimal? right)
        => left.HasValue || right.HasValue
            ? (left ?? 0m) + (right ?? 0m)
            : null;

    private static string? FormatQuantity(decimal? cantidad, string? unidad)
    {
        if (!cantidad.HasValue && string.IsNullOrWhiteSpace(unidad))
        {
            return null;
        }

        var quantity = cantidad.HasValue
            ? MarkdownV2Escaper.Escape(cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture))
            : string.Empty;

        if (string.IsNullOrWhiteSpace(unidad))
        {
            return quantity;
        }

        return string.IsNullOrWhiteSpace(quantity)
            ? MarkdownV2Escaper.Escape(unidad.Trim())
            : $"{quantity} {MarkdownV2Escaper.Escape(unidad.Trim())}";
    }
}

public static class ListaComprasDefaults
{
    public const string ManualGroupName = "Productos agregados";
}
