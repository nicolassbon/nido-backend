using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Application.Alacena;
using Nido.Application.Common.Security;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alacena")]
public sealed class AlacenaController : ControllerBase
{
    private readonly GetStockItemsHandler _getStockItemsHandler;
    private readonly CreateStockItemHandler _createStockItemHandler;
    private readonly UpdateStockItemHandler _updateStockItemHandler;
    private readonly DeleteStockItemHandler _deleteStockItemHandler;

    public AlacenaController(
        GetStockItemsHandler getStockItemsHandler,
        CreateStockItemHandler createStockItemHandler,
        UpdateStockItemHandler updateStockItemHandler,
        DeleteStockItemHandler deleteStockItemHandler)
    {
        _getStockItemsHandler = getStockItemsHandler;
        _createStockItemHandler = createStockItemHandler;
        _updateStockItemHandler = updateStockItemHandler;
        _deleteStockItemHandler = deleteStockItemHandler;
    }

    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var items = await _getStockItemsHandler.Handle(
            new GetStockItemsQuery(currentUser.HogarId), ct);

        return Ok(items.Select(ToResponse));
    }

    [HttpPost("productos")]
    public async Task<IActionResult> CreateProducto(
        [FromBody] CreateStockItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var created = await _createStockItemHandler.Handle(
            new CreateStockItemCommand(
                currentUser.HogarId,
                currentUser.UsuarioId,
                request.Nombre,
                request.CodigoBarras,
                request.Imagen,
                request.Ubicacion,
                request.Cantidad,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido),
            ct);

        return CreatedAtAction(nameof(GetProductos), ToResponse(created));
    }

    [HttpPatch("productos/{id:guid}")]
    public async Task<IActionResult> UpdateProducto(
        Guid id,
        [FromBody] UpdateStockItemRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var updated = await _updateStockItemHandler.Handle(
            new UpdateStockItemCommand(
                id,
                currentUser.UsuarioId,
                request.Cantidad,
                request.Ubicacion,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido),
            ct);

        if (updated is null) return NotFound();

        return Ok(ToResponse(updated));
    }

    [HttpDelete("productos/{id:guid}")]
    public async Task<IActionResult> DeleteProducto(
        Guid id,
        CancellationToken ct)
    {
        var deleted = await _deleteStockItemHandler.Handle(
            new DeleteStockItemCommand(id), ct);

        if (!deleted) return NotFound();

        return NoContent();
    }

    private static StockItemResponse ToResponse(StockItemResult item) =>
        new(
            item.Id,
            item.ProductoId,
            item.Nombre,
            item.Imagen,
            item.CodigoBarras,
            item.Ubicacion,
            item.Cantidad,
            item.FechaVencimiento,
            item.EstaAbierto,
            item.PorcentajeConsumido
        );
}
