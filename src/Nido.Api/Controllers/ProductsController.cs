using Microsoft.AspNetCore.Mvc;
using Nido.Application.Productos;
using Nido.Api.Contracts.Productos;

namespace Nido.Api.Controllers;

[ApiController]
[Route("productos")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateProductoHandler _createProductoHandler;

    public ProductsController(
        CreateProductoHandler createProductoHandler)
    {
        _createProductoHandler = createProductoHandler;
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
