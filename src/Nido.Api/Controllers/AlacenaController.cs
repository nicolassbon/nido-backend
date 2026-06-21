using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Application.Alacena;
using Nido.Application.Common.Security;
using Nido.Application.Insights;
using Nido.Infrastructure.Alacena;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alacena")]
public sealed class AlacenaController : ControllerBase
{
    private readonly GetStockItemsHandler _getStockItemsHandler;
    private readonly GetStockItemByIdHandler _getStockItemByIdHandler;
    private readonly CreateStockItemHandler _createStockItemHandler;
    private readonly UpdateStockItemHandler _updateStockItemHandler;
    private readonly DeleteStockItemHandler _deleteStockItemHandler;
    private readonly GetStockMovementsHandler _getStockMovementsHandler;
    private readonly CatalogoRepository _catalogoRepository;

    public AlacenaController(
        GetStockItemsHandler getStockItemsHandler,
        GetStockItemByIdHandler getStockItemByIdHandler,
        CreateStockItemHandler createStockItemHandler,
        UpdateStockItemHandler updateStockItemHandler,
        DeleteStockItemHandler deleteStockItemHandler,
        GetStockMovementsHandler getStockMovementsHandler,
        CatalogoRepository catalogoRepository)
    {
        _getStockItemsHandler = getStockItemsHandler;
        _getStockItemByIdHandler = getStockItemByIdHandler;
        _createStockItemHandler = createStockItemHandler;
        _updateStockItemHandler = updateStockItemHandler;
        _deleteStockItemHandler = deleteStockItemHandler;
        _getStockMovementsHandler = getStockMovementsHandler;
        _catalogoRepository = catalogoRepository;
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

    [HttpGet("productos/{id:guid}")]
    public async Task<IActionResult> GetProducto(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var item = await _getStockItemByIdHandler.Handle(
            new GetStockItemByIdQuery(id, currentUser.HogarId), ct);

        if (item is null) return NotFound();

        return Ok(ToResponse(item));
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
                request.CategoriaId,
                request.CodigoBarras,
                request.Imagen,
                request.Ubicacion,
                request.Cantidad,
                request.UnidadMedida,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido,
                CantidadEnvases: request.CantidadEnvases ?? 1,
                OrigenCarga: request.OrigenCarga,
                Calorias: request.Calorias,
                Proteinas: request.Proteinas,
                Carbohidratos: request.Carbohidratos,
                Grasas: request.Grasas),
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
                currentUser.HogarId,
                request.Nombre,
                request.Cantidad,
                request.Ubicacion,
                request.UnidadMedida,
                request.FechaVencimiento,
                request.EstaAbierto,
                request.PorcentajeConsumido,
                CantidadEnvases: request.CantidadEnvases),
            ct);

        if (updated is null) return NotFound();

        return Ok(ToResponse(updated));
    }

    [HttpDelete("productos/{id:guid}")]
    public async Task<IActionResult> DeleteProducto(
        Guid id,
        [FromQuery] string? motivo,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        if (!TryMapDeleteMotivo(motivo, out var motivoConsumo))
        {
            return BadRequest(new { message = "El motivo debe ser 'consumido', 'descartado' o 'vencido'." });
        }

        var deleted = await _deleteStockItemHandler.Handle(
            new DeleteStockItemCommand(id, currentUser.HogarId, currentUser.UsuarioId, motivoConsumo), ct);

        if (!deleted) return NotFound();

        return NoContent();
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> GetMovimientos(
        [FromQuery] string? motivo,
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] string? q,
        [FromQuery] int? limit,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        if (!TryMapMovimientoMotivo(motivo, out var motivoConsumo))
        {
            return BadRequest(new { message = "El motivo debe ser 'consumido', 'descartado', 'vencido' o 'cocinado'." });
        }

        if (!TryParseDate(desde, out var desdeDate))
        {
            return BadRequest(new { message = "La fecha 'desde' debe tener formato yyyy-MM-dd." });
        }

        if (!TryParseDate(hasta, out var hastaDate))
        {
            return BadRequest(new { message = "La fecha 'hasta' debe tener formato yyyy-MM-dd." });
        }

        var result = await _getStockMovementsHandler.Handle(
            new GetStockMovementsQuery(
                currentUser.HogarId,
                motivoConsumo,
                desdeDate,
                hastaDate,
                q,
                limit ?? 100),
            ct);

        return Ok(result.Select(ToMovementResponse));
    }

    private static StockItemResponse ToResponse(StockItemResult item) =>
        new(
            item.Id,
            item.ProductoId,
            item.Nombre,
            item.Imagen,
            item.CodigoBarras,
            item.CategoriaNombre,
            item.Ubicacion,
            item.Cantidad,
            item.UnidadMedida,
            item.FechaVencimiento,
            item.EstaAbierto,
            item.PorcentajeConsumido,
            item.CantidadEnvases,
            item.OrigenCarga,
            item.Calorias,
            item.Proteinas,
            item.Carbohidratos,
            item.Grasas
        );

    private static bool TryMapDeleteMotivo(string? motivo, out string? motivoConsumo)
    {
        motivoConsumo = null;
        if (string.IsNullOrWhiteSpace(motivo)) return true;

        motivoConsumo = motivo.Trim().ToLowerInvariant() switch
        {
            "consumido" => ConsumoMotivos.Consumido,
            "descartado" => ConsumoMotivos.Descartado,
            "vencido" => ConsumoMotivos.Vencido,
            "terminado" => ConsumoMotivos.Consumido,
            _ => null
        };

        return motivoConsumo is not null;
    }

    private static bool TryMapMovimientoMotivo(string? motivo, out string? motivoConsumo)
    {
        motivoConsumo = null;
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        motivoConsumo = motivo.Trim().ToLowerInvariant() switch
        {
            "consumido" => ConsumoMotivos.Consumido,
            "descartado" => ConsumoMotivos.Descartado,
            "vencido" => ConsumoMotivos.Vencido,
            "cocinado" => ConsumoMotivos.Cocinado,
            "terminado" => ConsumoMotivos.Consumido,
            _ => null
        };

        return motivoConsumo is not null;
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", out var parsed))
        {
            return false;
        }

        date = parsed;
        return true;
    }


    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias(CancellationToken ct)
    {
        var result = await _catalogoRepository.GetCategoriasAsync(ct);
        return Ok(result.Select(c => new { c.Id, c.Nombre, c.TtlDias }));
    }

    [HttpGet("unidades-medida")]
    public async Task<IActionResult> GetUnidadesMedida(CancellationToken ct)
    {
        var result = await _catalogoRepository.GetUnidadesMedidaAsync(ct);
        return Ok(result.Select(u => new { u.Id, u.Codigo, u.Nombre }));
    }

    [HttpGet("ubicaciones")]
    public async Task<IActionResult> GetUbicaciones(CancellationToken ct)
    {
        var result = await _catalogoRepository.GetUbicacionesAsync(ct);
        return Ok(result.Select(u => new { u.Id, u.Nombre, u.Icono, u.Color }));
    }
    private static StockMovementResponse ToMovementResponse(StockMovementResult item) =>
        new(
            item.Id,
            item.ProductoId,
            item.ProductoNombre,
            item.Cantidad,
            item.UnidadMedida,
            item.Motivo,
            item.FechaConsumo,
            item.UsuarioId);
}

