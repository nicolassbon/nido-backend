using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Api.Contracts.Productos;
using Nido.Application.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductoController : ControllerBase
{
    private readonly GetProductByBarcodeHandler _getByBarcodeHandler;

    public ProductoController(
        GetProductByBarcodeHandler getByBarcodeHandler)
    {
        _getByBarcodeHandler = getByBarcodeHandler;
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(
        string barcode,
        CancellationToken ct)
    {
        var producto = await _getByBarcodeHandler.Handle(
            new GetProductByBarcodeQuery(barcode),
            ct);

        if (producto is null)
        {
            return NotFound();
        }

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