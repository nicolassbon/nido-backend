using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Alacena;
using Nido.Api.Contracts.Productos;
using Nido.Application.Common.Security;
using Nido.Application.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/productos")]
public sealed class ProductoController : ControllerBase
{
    private readonly GetProductByBarcodeHandler        _getByBarcodeHandler;
    private readonly SearchProductosHandler            _searchHandler;
    private readonly LookupExternalProductoHandler     _externalLookupHandler;

    public ProductoController(
        GetProductByBarcodeHandler getByBarcodeHandler,
        SearchProductosHandler searchHandler,
        LookupExternalProductoHandler externalLookupHandler)
    {
        _getByBarcodeHandler   = getByBarcodeHandler;
        _searchHandler         = searchHandler;
        _externalLookupHandler = externalLookupHandler;
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(
        string barcode,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var producto = await _getByBarcodeHandler.Handle(
            new GetProductByBarcodeQuery(barcode, currentUser.HogarId),
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
            producto.TtlDias,
            producto.Gramaje,
            producto.UnidadMedida,
            producto.Calorias,
            producto.Proteinas,
            producto.Carbohidratos,
            producto.Grasas
        ));
    }

    [HttpGet("external-lookup/{barcode}")]
    public async Task<IActionResult> ExternalLookup(
        string barcode,
        CancellationToken ct)
    {
        LookupExternalProductoResult result;
        try
        {
            result = await _externalLookupHandler.Handle(
                new LookupExternalProductoQuery(barcode),
                ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new
        {
            name              = result.Name,
            image             = result.Image,
            brands            = result.Brands,
            categoriesTags    = result.CategoriesTags,
            categoriaSugerida = result.CategoriaSugerida,
            foundInDb         = result.FoundInDb,
            calorias          = result.Calorias,
            proteinas         = result.Proteinas,
            carbohidratos     = result.Carbohidratos,
            grasas            = result.Grasas,
            gramajeExtraido   = result.GramajeExtraido,
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        CancellationToken ct)
    {
        var results = await _searchHandler.Handle(new SearchProductosQuery(q), ct);
        return Ok(results.Select(r => new
        {
            r.Nombre,
            r.CategoriaNombre,
            r.CategoriaId,
            r.UnidadMedida,
            r.Ubicacion,
        }));
    }
}