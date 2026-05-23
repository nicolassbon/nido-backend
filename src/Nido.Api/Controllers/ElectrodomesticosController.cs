using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Electrodomesticos;
using Nido.Application.Electrodomesticos;

namespace Nido.Api.Controllers;

[ApiController]
[Route("electrodomesticos")]
public sealed class ElectrodomesticosController : ControllerBase
{
    private readonly CreateElectrodomesticoHandler _createHandler;
    private readonly GetElectrodomesticosHandler _getHandler;

    public ElectrodomesticosController(
        CreateElectrodomesticoHandler createHandler,
        GetElectrodomesticosHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _getHandler.Handle(cancellationToken);

        var response = result.Select(electrodomestico => new ElectrodomesticoResponse(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado
        ));

        return Ok(response);
    }

    [HttpGet("hogar/{hogarId}")]
    public async Task<IActionResult> GetByHogar(Guid hogarId, CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleByHogar(hogarId, cancellationToken);

        var response = result.Select(electrodomestico => new ElectrodomesticoResponse(
            electrodomestico.Id,
            electrodomestico.HogarId,
            electrodomestico.Nombre,
            electrodomestico.Tipo,
            electrodomestico.Estado
        ));

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] CreateElectrodomesticoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.HogarId == Guid.Empty)
        {
            return Problem(
                title: "Invalid household",
                detail: "HogarId is required.",
                statusCode: StatusCodes.Status400BadRequest);
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
                    request.HogarId,
                    request.Nombre,
                    request.Tipo,
                    request.Estado
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
                    result.Estado
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