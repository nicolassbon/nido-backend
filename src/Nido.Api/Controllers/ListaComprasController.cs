using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.ListaCompras;
using Nido.Application.Common.Security;
using Nido.Application.ListaCompras;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lista-compras")]
public sealed class ListaComprasController : ControllerBase
{
    private readonly GetListaComprasHandler _getHandler;
    private readonly GetListaComprasHistorialHandler _historialHandler;
    private readonly AddListaCompraGroupHandler _addGroupHandler;
    private readonly AddListaCompraItemHandler _addItemHandler;
    private readonly MarkListaCompraItemCompradoHandler _markCompradoHandler;
    private readonly MarkListaCompraItemCompradoByNameHandler _markCompradoByNameHandler;
    private readonly RemoveListaCompraItemHandler _removeItemHandler;
    private readonly ClearListaComprasHandler _clearHandler;

    public ListaComprasController(
        GetListaComprasHandler getHandler,
        GetListaComprasHistorialHandler historialHandler,
        AddListaCompraGroupHandler addGroupHandler,
        AddListaCompraItemHandler addItemHandler,
        MarkListaCompraItemCompradoHandler markCompradoHandler,
        MarkListaCompraItemCompradoByNameHandler markCompradoByNameHandler,
        RemoveListaCompraItemHandler removeItemHandler,
        ClearListaComprasHandler clearHandler)
    {
        _getHandler = getHandler;
        _historialHandler = historialHandler;
        _addGroupHandler = addGroupHandler;
        _addItemHandler = addItemHandler;
        _markCompradoHandler = markCompradoHandler;
        _markCompradoByNameHandler = markCompradoByNameHandler;
        _removeItemHandler = removeItemHandler;
        _clearHandler = clearHandler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("historial")]
    public async Task<IActionResult> GetHistorial(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _historialHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpPost("grupos")]
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

    [HttpPost("items")]
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

    [HttpPatch("items/{id:guid}/comprado")]
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

    [HttpPatch("items/comprado-por-nombre")]
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

    [HttpDelete("items/{id:guid}")]
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

    [HttpDelete]
    public async Task<IActionResult> Clear(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await _clearHandler.Handle(new ClearListaComprasCommand(currentUser.HogarId), ct);
        return NoContent();
    }

    private static ListaCompraGrupoResponse ToResponse(ListaCompraGrupoResult group)
        => new(group.GrupoNombre, group.Items.Select(ToResponse).ToList());

    private static ListaCompraItemResponse ToResponse(ListaCompraItemResult item)
        => new(item.Id, item.ProductoId, item.Nombre, item.Cantidad, item.Unidad, item.Comprado, item.CompradoEn, item.Orden);

    private static ListaCompraHistorialItemResponse ToResponse(ListaCompraHistorialItemResult item)
        => new(item.Id, item.ProductoId, item.Nombre, item.Cantidad, item.Unidad, item.GrupoNombre, item.CompradoEn, item.CompradoPor);
}

