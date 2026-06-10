using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Common.Security;
using Nido.Application.Productos;
using Nido.Api.Contracts.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/productos")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateStockHomeHandler _createStockHomeHandler;
    private readonly GetProductManualHandler _getProductManualHandler;

    public ProductsController(
        CreateStockHomeHandler createStockHomeHandler,
        GetProductManualHandler getProductManualHandler)
    {
        _createStockHomeHandler = createStockHomeHandler;
        _getProductManualHandler = getProductManualHandler;
    }


    [HttpGet("manual")]
    public async Task<IActionResult> GetManual(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var products = await _getProductManualHandler.Handle(
            new GetProductManualCommand(currentUser.HogarId),
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
            product.PorcentajeConsumido,
            product.CantidadEnvases
        ));

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStockHomeRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _createStockHomeHandler.Handle(
            new CreateStockHomeCommand(
                request.Nombre,
                request.CategoriaId,
                request.Ubicacion,
                request.Cantidad,
                request.UnidadMedida ?? string.Empty,
                request.FechaVencimiento,
                currentUser.HogarId,
                currentUser.UsuarioId,
                CantidadEnvases: request.CantidadEnvases ?? 1),
            ct);

        return Ok(new CreateStockHomeResponse(
            result.StockHogarId,
            result.ProductoId,
            result.CantidadActual,
            result.UnidadMedida,
            result.FechaVencimiento,
            result.UsuarioIngresoId,
            result.Ubicacion,
            result.EstaAbierto,
            result.PorcentajeConsumido,
            result.CategoriaId,
            result.CantidadEnvases
        ));
    }
}
