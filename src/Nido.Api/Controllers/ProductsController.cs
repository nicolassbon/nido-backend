using Microsoft.AspNetCore.Mvc;
using Nido.Application.Productos;
using Nido.Api.Contracts.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Route("productos")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateProductoHandler _createProductoHandler;
private readonly GetProductManualHandler _getProductManualHandler;

public ProductsController(
    CreateProductoHandler createProductoHandler,
    GetProductManualHandler getProductManualHandler)
{
    _createProductoHandler = createProductoHandler;
    _getProductManualHandler = getProductManualHandler;
}


[HttpGet("manual")]
public async Task<IActionResult> GetManual(
    [FromQuery] Guid hogarId,
    CancellationToken ct)
{
    var products = await _getProductManualHandler.Handle(
        new GetProductManualCommand(hogarId),
        ct);

    var response = products.Select(product => new GetProductManualResponse(
        product.StockHogarId,
        product.ProductoId,
        product.Nombre,
        product.CategoriaId,
        product.CategoriaNombre,
        product.CodigoBarras,
        product.ImagenUrl,
        product.Ubicacion,
        product.Cantidad,
        product.UnidadMedida,
        product.FechaVencimiento,
        product.EstaAbierto,
        product.PorcentajeConsumido
    ));

    return Ok(response);
}

  [HttpPost]
    public async Task<IActionResult> Create(CreateProductoRequest request, CancellationToken ct)
    {
        var result = await _createProductoHandler.Handle(
            new CreateProductoCommand(request.Nombre ?? string.Empty, request.CategoriaId, request.Cantidad,
                request.UnidadMedida ?? string.Empty, request.FechaVencimiento, request.HogarId, request.UsuarioId), 
                ct);

        return Ok(new CreateProductoResponse(
            result.ProductoId,
            result.Nombre,
            result.Cantidad,
            result.UnidadMedida
        ));
    }



    
    }
