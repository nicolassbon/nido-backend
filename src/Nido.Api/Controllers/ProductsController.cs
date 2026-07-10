using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nido.Application.Common.Security;
using Nido.Application.Productos;
using Nido.Api.Contracts.Productos;
using Nido.Api.ImageUploads;
using Nido.Application.Productos.UploadProductImage;
using Nido.Infrastructure.Storage;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/productos")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateStockHomeHandler _createStockHomeHandler;
    private readonly GetProductManualHandler _getProductManualHandler;
    private readonly UploadProductImageHandler _uploadProductImageHandler;
    private readonly IOptions<SpacesOptions> _spacesOptions;

    public ProductsController(
        CreateStockHomeHandler createStockHomeHandler,
        GetProductManualHandler getProductManualHandler,
        UploadProductImageHandler uploadProductImageHandler,
        IOptions<SpacesOptions> spacesOptions)
    {
        _createStockHomeHandler = createStockHomeHandler;
        _getProductManualHandler = getProductManualHandler;
        _uploadProductImageHandler = uploadProductImageHandler;
        _spacesOptions = spacesOptions;
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
            product.CantidadEnvases,
            product.IconoSvg,
            product.Icono,
            product.CantidadCompraEstandar,
            product.UnidadCompraEstandar
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
                CantidadEnvases: request.CantidadEnvases ?? 1,
                Calorias: request.Calorias,
                Proteinas: request.Proteinas,
                Carbohidratos: request.Carbohidratos,
                Grasas: request.Grasas),
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

    [HttpPost("{id:guid}/imagen")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
        Guid id,
        [FromForm(Name = "imagen")] IFormFile? imagen,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var upload = await ImageUploadFormReader.ReadAsync(imagen, _spacesOptions.Value.MaxUploadBytes, cancellationToken);
        var result = await _uploadProductImageHandler.Handle(
            new UploadProductImageCommand(id, currentUser.HogarId, upload),
            cancellationToken);

        return Ok(new { imagenUrl = result.ImagenUrl });
    }
}
