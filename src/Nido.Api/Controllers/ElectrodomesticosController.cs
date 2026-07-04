using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nido.Api.Contracts.Electrodomesticos;
using Nido.Api.ImageUploads;
using Nido.Application.Common.Security;
using Nido.Application.Electrodomesticos;
using Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;
using Nido.Infrastructure.Storage;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/electrodomesticos")]
public sealed class ElectrodomesticosController : ControllerBase
{
    private readonly CreateElectrodomesticoHandler _createHandler;
    private readonly UpdateElectrodomesticoHandler _updateHandler;
    private readonly DeleteElectrodomesticoHandler _deleteHandler;
    private readonly GetElectrodomesticosHandler _getHandler;
    private readonly GetElectrodomesticosCatalogoHandler _getCatalogoHandler;
    private readonly UploadElectrodomesticoImageHandler _uploadImageHandler;
    private readonly IOptions<SpacesOptions> _spacesOptions;


    public ElectrodomesticosController(
     CreateElectrodomesticoHandler createHandler,
     UpdateElectrodomesticoHandler updateHandler,
     DeleteElectrodomesticoHandler deleteHandler,
     GetElectrodomesticosHandler getHandler,
     GetElectrodomesticosCatalogoHandler getCatalogoHandler,
     UploadElectrodomesticoImageHandler uploadImageHandler,
     IOptions<SpacesOptions> spacesOptions)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _getCatalogoHandler = getCatalogoHandler;
        _uploadImageHandler = uploadImageHandler;
        _spacesOptions = spacesOptions;
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
            electrodomestico.ImagenUrl,
            electrodomestico.CatalogoId
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
            electrodomestico.ImagenUrl,
            electrodomestico.CatalogoId
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

        if (!request.CatalogoId.HasValue && string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Problem(
                title: "Invalid appliance name",
                detail: "Nombre is required when CatalogoId is not provided.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await _createHandler.Handle(
                new CreateElectrodomesticoCommand(
                    currentUser.HogarId,
                    request.CatalogoId,
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
                    result.ImagenUrl,
                    result.CatalogoId
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

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        Guid id,
        [FromBody] UpdateElectrodomesticoRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var result = await _updateHandler.Handle(
            new UpdateElectrodomesticoCommand(id, currentUser.HogarId, request.Tipo, request.Estado),
            cancellationToken);

        if (result is null) return NotFound();

        return Ok(new ElectrodomesticoResponse(
            result.Id,
            result.HogarId,
            result.Nombre,
            result.Tipo,
            result.Estado,
            result.Marca,
            result.ImagenUrl,
            result.CatalogoId
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var deleted = await _deleteHandler.Handle(
            new DeleteElectrodomesticoCommand(id, currentUser.HogarId), cancellationToken);

        return deleted ? NoContent() : NotFound();
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
        var result = await _uploadImageHandler.Handle(
            new UploadElectrodomesticoImageCommand(id, currentUser.HogarId, upload),
            cancellationToken);

        return Ok(new { imagenUrl = result.ImagenUrl });
    }
}
