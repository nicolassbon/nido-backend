using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.ListaCompras;
using Nido.Application.Common.Security;
using Nido.Application.ListaCompras;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/listas-compra")]
public sealed class ListaComprasController : ControllerBase
{
    private readonly GetListasCompraHandler _getListasHandler;
    private readonly CreateListaCompraHandler _createListaHandler;
    private readonly UpdateListaCompraHandler _updateListaHandler;
    private readonly DeleteListaCompraHandler _deleteListaHandler;
    private readonly AddListaCompraNamedItemHandler _addNamedItemHandler;
    private readonly UpdateListaCompraItemHandler _updateItemHandler;
    private readonly RemoveListaCompraNamedItemHandler _removeNamedItemHandler;
    private readonly GetListaComprasHandler _getHandler;
    private readonly GetListaComprasHistorialHandler _historialHandler;
    private readonly AddListaCompraGroupHandler _addGroupHandler;
    private readonly AddListaCompraItemHandler _addItemHandler;
    private readonly MarkListaCompraItemCompradoHandler _markCompradoHandler;
    private readonly MarkListaCompraItemCompradoByNameHandler _markCompradoByNameHandler;
    private readonly MarkListaCompraItemAgregadoInventarioHandler _markAgregadoInventarioHandler;
    private readonly RemoveListaCompraItemHandler _removeItemHandler;
    private readonly ClearListaComprasHandler _clearHandler;

    public ListaComprasController(
        GetListasCompraHandler getListasHandler,
        CreateListaCompraHandler createListaHandler,
        UpdateListaCompraHandler updateListaHandler,
        DeleteListaCompraHandler deleteListaHandler,
        AddListaCompraNamedItemHandler addNamedItemHandler,
        UpdateListaCompraItemHandler updateItemHandler,
        RemoveListaCompraNamedItemHandler removeNamedItemHandler,
        GetListaComprasHandler getHandler,
        GetListaComprasHistorialHandler historialHandler,
        AddListaCompraGroupHandler addGroupHandler,
        AddListaCompraItemHandler addItemHandler,
        MarkListaCompraItemCompradoHandler markCompradoHandler,
        MarkListaCompraItemCompradoByNameHandler markCompradoByNameHandler,
        MarkListaCompraItemAgregadoInventarioHandler markAgregadoInventarioHandler,
        RemoveListaCompraItemHandler removeItemHandler,
        ClearListaComprasHandler clearHandler)
    {
        _getListasHandler = getListasHandler;
        _createListaHandler = createListaHandler;
        _updateListaHandler = updateListaHandler;
        _deleteListaHandler = deleteListaHandler;
        _addNamedItemHandler = addNamedItemHandler;
        _updateItemHandler = updateItemHandler;
        _removeNamedItemHandler = removeNamedItemHandler;
        _getHandler = getHandler;
        _historialHandler = historialHandler;
        _addGroupHandler = addGroupHandler;
        _addItemHandler = addItemHandler;
        _markCompradoHandler = markCompradoHandler;
        _markCompradoByNameHandler = markCompradoByNameHandler;
        _markAgregadoInventarioHandler = markAgregadoInventarioHandler;
        _removeItemHandler = removeItemHandler;
        _clearHandler = clearHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetListas(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getListasHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLista(
        [FromBody] CreateListaCompraRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        try
        {
            var result = await _createListaHandler.Handle(
                new CreateListaCompraCommand(currentUser.HogarId, currentUser.UsuarioId, request.Nombre),
                ct);

            return Ok(ToResponse(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateLista(
        Guid id,
        [FromBody] UpdateListaCompraRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        try
        {
            var result = await _updateListaHandler.Handle(
                new UpdateListaCompraCommand(currentUser.HogarId, id, request.Nombre),
                ct);

            return result is null ? NotFound() : Ok(ToResponse(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLista(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var removed = await _deleteListaHandler.Handle(new DeleteListaCompraCommand(currentUser.HogarId, id), ct);
        return removed ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddNamedItem(
        Guid id,
        [FromBody] AddListaCompraNamedItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        try
        {
            var result = await _addNamedItemHandler.Handle(
                new AddListaCompraNamedItemCommand(
                    currentUser.HogarId,
                    id,
                    currentUser.UsuarioId,
                    request.Nombre,
                    request.Cantidad,
                    request.Unidad),
                ct);

            return result is null ? NotFound() : Ok(ToResponse(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateNamedItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateListaCompraItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _updateItemHandler.Handle(
            new UpdateListaCompraItemCommand(
                currentUser.HogarId,
                id,
                itemId,
                currentUser.UsuarioId,
                request.Nombre,
                request.Cantidad,
                request.Unidad,
                request.Comprado),
            ct);

        return result is null ? NotFound() : Ok(ToResponse(result));
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveNamedItem(
        Guid id,
        Guid itemId,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var removed = await _removeNamedItemHandler.Handle(
            new RemoveListaCompraNamedItemCommand(itemId, currentUser.HogarId, id), ct);

        return removed ? NoContent() : NotFound();
    }

    [HttpGet("/api/lista-compras")]
    public async Task<IActionResult> GetLegacy(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("historial")]
    [HttpGet("/api/lista-compras/historial")]
    public async Task<IActionResult> GetHistorial(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _historialHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpPost("/api/lista-compras/grupos")]
    public async Task<IActionResult> AddGroup(
        [FromBody] AddListaCompraGroupRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var items = request.Items
            .Select(item => new ListaCompraItemInput(item.Nombre, item.Cantidad, item.Unidad))
            .ToList();

        var result = await _addGroupHandler.Handle(
            new AddListaCompraGroupCommand(currentUser.HogarId, currentUser.UsuarioId, request.GrupoNombre, items),
            ct);

        return Ok(result.Select(ToResponse));
    }

    [HttpPost("/api/lista-compras/items")]
    public async Task<IActionResult> AddItem(
        [FromBody] AddListaCompraItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        ListaCompraItemResult item;
        try
        {
            item = await _addItemHandler.Handle(
                new AddListaCompraItemCommand(
                    currentUser.HogarId,
                    currentUser.UsuarioId,
                    request.Nombre,
                    request.Cantidad,
                    request.Unidad,
                    request.GrupoNombre),
                ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(ToResponse(item));
    }

    [HttpPatch("/api/lista-compras/items/{id:guid}/comprado")]
    public async Task<IActionResult> MarkComprado(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _markCompradoHandler.Handle(
            new MarkListaCompraItemCompradoCommand(id, currentUser.HogarId, currentUser.UsuarioId),
            ct);

        return result is null ? NotFound() : Ok(ToResponse(result));
    }

    [HttpPatch("/api/lista-compras/items/comprado-por-nombre")]
    public async Task<IActionResult> MarkCompradoByName(
        [FromBody] MarkListaCompraItemByNameRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _markCompradoByNameHandler.Handle(
            new MarkListaCompraItemCompradoByNameCommand(currentUser.HogarId, currentUser.UsuarioId, request.Nombre),
            ct);

        return Ok(result.Select(ToResponse));
    }

    [HttpPatch("/api/lista-compras/items/{id:guid}/agregado-inventario")]
    public async Task<IActionResult> MarkAgregadoInventario(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var updated = await _markAgregadoInventarioHandler.Handle(
            new MarkListaCompraItemAgregadoInventarioCommand(id, currentUser.HogarId),
            ct);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("/api/lista-compras/items/{id:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var removed = await _removeItemHandler.Handle(
            new RemoveListaCompraItemCommand(id, currentUser.HogarId),
            ct);

        return removed ? NoContent() : NotFound();
    }

    [HttpDelete("/api/lista-compras")]
    public async Task<IActionResult> Clear(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await _clearHandler.Handle(new ClearListaComprasCommand(currentUser.HogarId), ct);
        return NoContent();
    }

    private static ListaCompraGrupoResponse ToResponse(ListaCompraGrupoResult group)
        => new(group.GrupoNombre, group.Items.Select(ToResponse).ToList());

    private static ListaCompraResponse ToResponse(ListaCompraListResult list)
        => new(list.Id, list.Nombre, list.CreatedAt, list.UpdatedAt, list.Items.Select(ToResponse).ToList());

    private static ListaCompraItemResponse ToResponse(ListaCompraItemResult item)
        => new(item.Id, item.ProductoId, item.Nombre, item.Cantidad, item.Unidad, item.Comprado, item.CompradoEn, item.Orden);

    private static ListaCompraHistorialItemResponse ToResponse(ListaCompraHistorialItemResult item)
        => new(item.Id, item.ProductoId, item.Nombre, item.Cantidad, item.Unidad, item.GrupoNombre, item.CompradoEn, item.CompradoPor);
}
