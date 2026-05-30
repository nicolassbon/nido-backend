using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Application.Alacena;

namespace Nido.Api.Controllers;

[ApiController]
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

    // ── GET api/alacena/productos?hogarId={guid} ───────────────────────────
    // TODO: hogarId will come from JWT claims (HttpContext.User) once auth is implemented
    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos([FromQuery] Guid hogarId, CancellationToken ct)
    {
        var items = await _getStockItemsHandler.Handle(new GetStockItemsQuery(hogarId), ct);

        return Ok(items.Select(ToResponse));
    }

    // ── POST api/alacena/productos ─────────────────────────────────────────
    // TODO: HogarId and UsuarioId will come from JWT claims once auth is implemented
    [HttpPost("productos")]
    public async Task<IActionResult> CreateProducto(
        [FromBody] CreateStockItemRequest request,
        CancellationToken ct)
    {
        var created = await _createStockItemHandler.Handle(
            new CreateStockItemCommand(
                request.HogarId,
                request.UsuarioId,
                request.Nombre,
                request.CodigoBarras,
                request.Imagen,
                request.Ubicacion,
                request.Cantidad,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido),
            ct);

        var response = ToResponse(created);
        return CreatedAtAction(
            nameof(GetProductos),
            new { hogarId = request.HogarId },
            response);
    }

    // ── PATCH api/alacena/productos/{id} ──────────────────────────────────
    // TODO: UsuarioId will come from JWT claims once auth is implemented
    [HttpPatch("productos/{id:guid}")]
    public async Task<IActionResult> UpdateProducto(
        Guid id,
        [FromBody] UpdateStockItemRequest request,
        CancellationToken ct)
    {
        var updated = await _updateStockItemHandler.Handle(
            new UpdateStockItemCommand(
                id,
                request.UsuarioId,
                request.Cantidad,
                request.Ubicacion,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido),
            ct);

        if (updated is null) return NotFound();

        return Ok(ToResponse(updated));
    }

    // ── DELETE api/alacena/productos/{id} ─────────────────────────────────
    [HttpDelete("productos/{id:guid}")]
    public async Task<IActionResult> DeleteProducto(Guid id, CancellationToken ct)
    {
        var deleted = await _deleteStockItemHandler.Handle(new DeleteStockItemCommand(id), ct);
        if (!deleted) return NotFound();

        return NoContent();
    }

    // ── Private ───────────────────────────────────────────────────────────
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
