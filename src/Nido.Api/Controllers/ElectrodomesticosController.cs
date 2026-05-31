using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Electrodomesticos;
using Nido.Application.Common.Security;
using Nido.Application.Electrodomesticos;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("electrodomesticos")]
public sealed class ElectrodomesticosController : ControllerBase
{
    private readonly CreateElectrodomesticoHandler _createHandler;
    private readonly GetElectrodomesticosHandler _getHandler;
    private readonly GetElectrodomesticosCatalogoHandler _getCatalogoHandler;


   public ElectrodomesticosController(
    CreateElectrodomesticoHandler createHandler,
    GetElectrodomesticosHandler getHandler,
    GetElectrodomesticosCatalogoHandler getCatalogoHandler)
{
    _createHandler = createHandler;
    _getHandler = getHandler;
    _getCatalogoHandler = getCatalogoHandler;
}

[HttpGet("catalogo")]
public async Task<IActionResult> GetCatalogo(CancellationToken cancellationToken)
{
    var result = await _getCatalogoHandler.Handle(cancellationToken);

    var response = result.Select(item => new ElectrodomesticoCatalogoResponse(
        item.Id,
        item.Nombre,
        item.Tipo,
        item.Icono,
        item.ImagenUrl,
        item.Orden
    ));

    return Ok(response);
}

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleByHogar(currentUser.HogarId, cancellationToken);

        var response = result.Select(electrodomestico => new ElectrodomesticoResponse(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado,
            electrodomestico.Marca,
            electrodomestico.ImagenUrl
        ));

        return Ok(response);
    }

    [HttpGet("hogar/{hogarId}")]
    public async Task<IActionResult> GetByHogar(
        Guid hogarId,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (hogarId != currentUser.HogarId)
        {
            return Forbid();
        }

        var result = await _getHandler.HandleByHogar(hogarId, cancellationToken);

        var response = result.Select(electrodomestico => new ElectrodomesticoResponse(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado,
            electrodomestico.Marca,
            electrodomestico.ImagenUrl
        ));

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] CreateElectrodomesticoRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (request.HogarId != Guid.Empty && request.HogarId != currentUser.HogarId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Problem(
                title: "Invalid appliance name",
                detail: "Nombre is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await _createHandler.Handle(
                new CreateElectrodomesticoCommand(
                    currentUser.HogarId,
                    request.Nombre,
                    request.Tipo,
                    request.Estado,
                    request.Marca,
                    request.ImagenUrl
                ),
                cancellationToken
            );

            return StatusCode(
                StatusCodes.Status201Created,
                new ElectrodomesticoResponse(
                    result.Id,
                    result.HogarId,
                    result.Nombre,
                    result.Tipo,
                    result.Estado,
                    result.Marca,
                    result.ImagenUrl
                )
            );
        }
        catch (ArgumentException)
        {
            return Problem(
                title: "Invalid appliance",
                detail: "The appliance data is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException)
        {
            return Problem(
                title: "Household not found",
                detail: "The selected household does not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }
    }
}
