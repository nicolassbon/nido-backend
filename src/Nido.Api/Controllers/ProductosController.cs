using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Application.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductoController : ControllerBase
{
    private readonly GetProductByBarcodeHandler _handler;

    public ProductoController(GetProductByBarcodeHandler handler) => _handler = handler;

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken ct)
    {
        var producto = await _handler.Handle(new GetProductByBarcodeQuery(barcode), ct);

        if (producto is null) return NotFound();

        return Ok(new ProductoResponse(
            producto.Id,
            producto.Nombre,
            producto.CodigoBarras,
            producto.Imagen,
            producto.CategoriaNombre,
            producto.TtlDias
        ));
    }
}
