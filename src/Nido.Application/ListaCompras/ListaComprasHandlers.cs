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

public static class ListaComprasDefaults
{
    public const string ManualGroupName = "Productos agregados";
}

