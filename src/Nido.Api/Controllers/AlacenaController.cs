using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nido.Api.Contracts.Alacena;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/alacena")]
public sealed class AlacenaController : ControllerBase
{
    private readonly NidoDbContext _db;

    public AlacenaController(NidoDbContext db) => _db = db;

    // ── GET api/alacena/productos?hogarId={guid} ───────────────────────────
    // TODO: hogarId will come from JWT claims (HttpContext.User) once auth is implemented
    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos([FromQuery] Guid hogarId, CancellationToken ct)
    {
        var items = await _db.StockHogars
            .Where(s => s.HogarId == hogarId)
            .Include(s => s.Producto)
            .Select(s => new StockItemResponse(
                s.Id,
                s.ProductoId,
                s.Producto.Nombre,
                s.Producto.ImagenUrl,
                s.Producto.CodigoBarras,
                s.Ubicacion,
                s.CantidadActual ?? 0,
                s.FechaVencimiento.HasValue
                    ? s.FechaVencimiento.Value.ToString("yyyy-MM-dd")
                    : null,
                s.EstaAbierto,
                s.PorcentajeConsumido
            ))
            .ToListAsync(ct);

        return Ok(items);
    }

    // ── POST api/alacena/productos ─────────────────────────────────────────
    // TODO: HogarId and UsuarioId will come from JWT claims once auth is implemented
    [HttpPost("productos")]
    public async Task<IActionResult> CreateProducto(
        [FromBody] CreateStockItemRequest request,
        CancellationToken ct)
    {
        // 1. Find or create the product record
        Producto? producto = null;

        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            producto = await _db.Productos
                .FirstOrDefaultAsync(p => p.CodigoBarras == request.CodigoBarras, ct);
        }

        if (producto is null)
        {
            producto = new Producto
            {
                Nombre       = request.Nombre,
                CodigoBarras = request.CodigoBarras,
                ImagenUrl    = request.Imagen,
            };
            _db.Productos.Add(producto);
        }
        else if (string.IsNullOrWhiteSpace(producto.ImagenUrl) &&
                 !string.IsNullOrWhiteSpace(request.Imagen))
        {
            // Backfill image if we now have one
            producto.ImagenUrl = request.Imagen;
        }

        // 2. Parse expiry date
        DateOnly? fechaVenc = null;
        if (!string.IsNullOrWhiteSpace(request.FechaVencimiento) &&
            DateOnly.TryParse(request.FechaVencimiento, out var parsedDate))
        {
            fechaVenc = parsedDate;
        }

        // 3. Create stock entry
        var stockItem = new StockHogar
        {
            HogarId             = request.HogarId,
            Producto            = producto,
            CargadoPor          = request.UsuarioId,
            UpdatedBy           = request.UsuarioId,
            CantidadActual      = request.Cantidad,
            UnidadMedida        = "unidad",
            FechaVencimiento    = fechaVenc,
            Ubicacion           = request.Ubicacion,
            EstaAbierto         = request.EstaAbierto,
            PorcentajeConsumido = request.PorcentajeConsumido,
        };
        _db.StockHogars.Add(stockItem);

        await _db.SaveChangesAsync(ct);

        var response = ToResponse(stockItem, producto);
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
        var item = await _db.StockHogars
            .Include(s => s.Producto)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (item is null) return NotFound();

        if (request.Cantidad.HasValue)
            item.CantidadActual = request.Cantidad.Value;

        if (request.Ubicacion is not null)
            item.Ubicacion = request.Ubicacion;

        if (request.EstaAbierto.HasValue)
            item.EstaAbierto = request.EstaAbierto.Value;

        if (request.PorcentajeConsumido.HasValue)
            item.PorcentajeConsumido = request.PorcentajeConsumido.Value;

        if (request.FechaVencimiento is not null)
        {
            item.FechaVencimiento = DateOnly.TryParse(request.FechaVencimiento, out var d)
                ? d
                : null;
        }

        item.UpdatedBy = request.UsuarioId;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(ToResponse(item, item.Producto));
    }

    // ── DELETE api/alacena/productos/{id} ─────────────────────────────────
    [HttpDelete("productos/{id:guid}")]
    public async Task<IActionResult> DeleteProducto(Guid id, CancellationToken ct)
    {
        var item = await _db.StockHogars.FindAsync([id], ct);
        if (item is null) return NotFound();

        _db.StockHogars.Remove(item);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Private ───────────────────────────────────────────────────────────
    private static StockItemResponse ToResponse(StockHogar s, Producto p) =>
        new(
            s.Id,
            s.ProductoId,
            p.Nombre,
            p.ImagenUrl,
            p.CodigoBarras,
            s.Ubicacion,
            s.CantidadActual ?? 0,
            s.FechaVencimiento?.ToString("yyyy-MM-dd"),
            s.EstaAbierto,
            s.PorcentajeConsumido
        );
}
