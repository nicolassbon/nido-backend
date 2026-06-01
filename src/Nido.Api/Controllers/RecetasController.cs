using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Recetas;
using Nido.Application.Recetas;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("recetas")]
public sealed class RecetasController : ControllerBase
{
    private readonly GetRecetasHandler _getRecetasHandler;

    public RecetasController(GetRecetasHandler getRecetasHandler)
    {
        _getRecetasHandler = getRecetasHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _getRecetasHandler.Handle(ct);
        return Ok(result.Select(ToResponse));
    }

    private static RecetaResponse ToResponse(RecetaResult receta)
    {
        return new RecetaResponse(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            receta.ImagenUrl,
            receta.Calorias,
            receta.Proteinas,
            receta.Carbohidratos,
            receta.Grasas,
            receta.Ingredientes.Select(ingrediente => new RecetaIngredienteResponse(
                ingrediente.Id,
                ingrediente.ProductoId,
                ingrediente.Nombre,
                ingrediente.ProductoNombre,
                ingrediente.Cantidad,
                ingrediente.Unidad)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList());
    }
}
