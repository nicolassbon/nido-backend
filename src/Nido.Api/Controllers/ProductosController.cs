using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nido.Api.Contracts.Alacena;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController : ControllerBase
{
    private readonly NidoDbContext _db;

    public ProductosController(NidoDbContext db) => _db = db;

    // ── GET api/productos/barcode/{barcode} ────────────────────────────────
    // Frontend checks this BEFORE calling Open Food Facts.
    // If found → use our data (instant, no external call).
    // If 404   → fall through to Open Food Facts cascade.
    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken ct)
    {
        var producto = await _db.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.CodigoBarras == barcode, ct);

        if (producto is null) return NotFound();

        return Ok(new ProductoResponse(
            producto.Id,
            producto.Nombre,
            producto.CodigoBarras,
            producto.ImagenUrl,
            producto.Categoria?.Nombre,
            producto.Categoria?.TtlDias
        ));
    }
}
