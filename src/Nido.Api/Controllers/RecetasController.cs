using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Recetas;
using Nido.Application.Common.Security;
using Nido.Application.Recetas;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recetas")]
public sealed class RecetasController : ControllerBase
{
    private readonly GetRecetasHandler _getRecetasHandler;
    private readonly GetRecetaByIdHandler _getRecetaByIdHandler;
    private readonly CocinarRecetaHandler _cocinarRecetaHandler;

    public RecetasController(
        GetRecetasHandler getRecetasHandler,
        GetRecetaByIdHandler getRecetaByIdHandler,
        CocinarRecetaHandler cocinarRecetaHandler)
    {
        _getRecetasHandler = getRecetasHandler;
        _getRecetaByIdHandler = getRecetaByIdHandler;
        _cocinarRecetaHandler = cocinarRecetaHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetasHandler.Handle(currentUser.HogarId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetaByIdHandler.Handle(
            new GetRecetaByIdCommand(id, currentUser.HogarId), ct);

        if (result is null)
            return NotFound();

        return Ok(ToResponseFromById(result));
    }

    [HttpPost("{id}/cocinar")]
    public async Task<IActionResult> Cocinar(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _cocinarRecetaHandler.Handle(
            new CocinarRecetaCommand(id, currentUser.HogarId, currentUser.UsuarioId), ct);

        if (result is null)
            return NotFound();

        return Ok(new CocinarRecetaResponse(result.RecetaId, result.VecesCocinada));
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
                ingrediente.Unidad,
                ingrediente.EnStock)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada);
    }

    private static RecetaResponse ToResponseFromById(GetRecetaByIdResult receta)
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
                ingrediente.Unidad,
                ingrediente.EnStock)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada);
    }
}
