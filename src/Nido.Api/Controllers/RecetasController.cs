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
    private readonly GetRecetasHandler           _getRecetasHandler;
    private readonly GetRecetasGuardadasHandler  _getRecetasGuardadasHandler;
    private readonly GetRecetaByIdHandler        _getRecetaByIdHandler;
    private readonly SaveRecetaHandler           _saveRecetaHandler;
    private readonly UnsaveRecetaHandler         _unsaveRecetaHandler;
    private readonly CocinarRecetaHandler        _cocinarRecetaHandler;
    private readonly UpsertResenaHandler         _upsertResenaHandler;
    private readonly GetResenasByRecetaHandler   _getResenasHandler;
    private readonly DeleteResenaHandler         _deleteResenaHandler;
    private readonly AddNotaRecetaHandler        _addNotaHandler;
    private readonly DeleteNotaRecetaHandler     _deleteNotaHandler;
    private readonly GetNotasByRecetaHandler     _getNotasHandler;

    public RecetasController(
        GetRecetasHandler getRecetasHandler,
        GetRecetasGuardadasHandler getRecetasGuardadasHandler,
        GetRecetaByIdHandler getRecetaByIdHandler,
        SaveRecetaHandler saveRecetaHandler,
        UnsaveRecetaHandler unsaveRecetaHandler,
        CocinarRecetaHandler cocinarRecetaHandler,
        UpsertResenaHandler upsertResenaHandler,
        GetResenasByRecetaHandler getResenasHandler,
        DeleteResenaHandler deleteResenaHandler,
        AddNotaRecetaHandler addNotaHandler,
        DeleteNotaRecetaHandler deleteNotaHandler,
        GetNotasByRecetaHandler getNotasHandler)
    {
        _getRecetasHandler    = getRecetasHandler;
        _getRecetasGuardadasHandler = getRecetasGuardadasHandler;
        _getRecetaByIdHandler = getRecetaByIdHandler;
        _saveRecetaHandler    = saveRecetaHandler;
        _unsaveRecetaHandler  = unsaveRecetaHandler;
        _cocinarRecetaHandler = cocinarRecetaHandler;
        _upsertResenaHandler  = upsertResenaHandler;
        _getResenasHandler    = getResenasHandler;
        _deleteResenaHandler  = deleteResenaHandler;
        _addNotaHandler       = addNotaHandler;
        _deleteNotaHandler    = deleteNotaHandler;
        _getNotasHandler      = getNotasHandler;
    }

    [HttpGet("guardadas")]
    public async Task<IActionResult> GetGuardadas(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetasGuardadasHandler.Handle(currentUser.HogarId, currentUser.UsuarioId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetasHandler.Handle(currentUser.HogarId, currentUser.UsuarioId, ct);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecetaByIdHandler.Handle(
            new GetRecetaByIdCommand(id, currentUser.HogarId, currentUser.UsuarioId), ct);

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

    [HttpPost("{id:guid}/guardar")]
    public async Task<IActionResult> Guardar(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var saved = await _saveRecetaHandler.Handle(id, currentUser.HogarId, currentUser.UsuarioId, ct);
        return saved ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/guardar")]
    public async Task<IActionResult> QuitarGuardada(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var removed = await _unsaveRecetaHandler.Handle(id, currentUser.HogarId, ct);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("{id}/resenas")]
    public async Task<IActionResult> GetResenas(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getResenasHandler.Handle(
            new GetResenasByRecetaQuery(id, currentUser.HogarId, currentUser.UsuarioId), ct);

        return Ok(new ResenasRecetaResponse(
            result.Items.Select(ToResenaResponse).ToList(),
            result.Resumen.Promedio,
            result.Resumen.Total,
            result.MiResena is null ? null : ToResenaResponse(result.MiResena)));
    }

    [HttpPut("{id}/resenas")]
    public async Task<IActionResult> UpsertResena(
        Guid id,
        [FromBody] UpsertResenaRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        try
        {
            var resena = await _upsertResenaHandler.Handle(
                new UpsertResenaCommand(id, currentUser.HogarId, currentUser.UsuarioId, request.Puntuacion, request.Comentario), ct);

            return Ok(ToResenaResponse(resena));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/resenas")]
    public async Task<IActionResult> DeleteResena(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await _deleteResenaHandler.Handle(
            new DeleteResenaCommand(id, currentUser.HogarId, currentUser.UsuarioId), ct);
        return NoContent();
    }

    [HttpGet("{id}/notas")]
    public async Task<IActionResult> GetNotas(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getNotasHandler.Handle(
            new GetNotasByRecetaQuery(id, currentUser.HogarId), ct);
        return Ok(new NotasRecetaResponse(result.Items.Select(ToNotaResponse).ToList()));
    }

    [HttpPost("{id}/notas")]
    public async Task<IActionResult> AddNota(
        Guid id,
        [FromBody] AddNotaRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        try
        {
            var nota = await _addNotaHandler.Handle(
                new AddNotaRecetaCommand(id, currentUser.HogarId, currentUser.UsuarioId, request.Texto), ct);
            return Ok(ToNotaResponse(nota));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/notas/{notaId}")]
    public async Task<IActionResult> DeleteNota(
        Guid id,
        Guid notaId,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await _deleteNotaHandler.Handle(
            new DeleteNotaRecetaCommand(notaId, currentUser.HogarId, currentUser.UsuarioId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/imagen")]
    [Consumes("multipart/form-data")]
    public IActionResult UploadImage(
        Guid id,
        [FromForm(Name = "imagen")] IFormFile? imagen,
        CancellationToken cancellationToken)
    {
        return Forbid();
    }

    private static NotaResponse ToNotaResponse(NotaItem item) =>
        new(item.Id, item.RecetaId, item.HogarId, item.UsuarioId,
            item.UsuarioNombre, item.UsuarioFotoUrl, item.Texto, item.CreatedAt);

    private static ResenaResponse ToResenaResponse(ResenaItem item) =>
        new(item.Id, item.RecetaId, item.UsuarioId, item.UsuarioNombre, item.UsuarioFotoUrl,
            item.Puntuacion, item.Comentario, item.CreatedAt, item.UpdatedAt);

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
                ingrediente.EnStock,
                ingrediente.Alergenos)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada,
            receta.CalificacionPromedio,
            receta.CalificacionTotal,
            receta.TieneProductosPorVencer,
            receta.FechaVencimientoMasProxima,
            receta.DiasHastaVencimiento,
            receta.ProductosPorVencer.Select(producto => new RecetaProductoPorVencerResponse(
                producto.ProductoId,
                producto.Nombre,
                producto.FechaVencimiento,
                producto.DiasHastaVencimiento)).ToList(),
            receta.Guardada);
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
                ingrediente.EnStock,
                ingrediente.Alergenos)).ToList(),
            receta.Pasos.Select(paso => new RecetaPasoResponse(
                paso.Id,
                paso.Orden,
                paso.Descripcion)).ToList(),
            receta.Electrodomesticos.Select(electrodomestico => new RecetaElectrodomesticoResponse(
                electrodomestico.Id,
                electrodomestico.TipoRequerido)).ToList(),
            receta.VecesCocinada,
            receta.CalificacionPromedio,
            receta.CalificacionTotal,
            receta.TieneProductosPorVencer,
            receta.FechaVencimientoMasProxima,
            receta.DiasHastaVencimiento,
            receta.ProductosPorVencer.Select(producto => new RecetaProductoPorVencerResponse(
                producto.ProductoId,
                producto.Nombre,
                producto.FechaVencimiento,
                producto.DiasHastaVencimiento)).ToList(),
            receta.Guardada);
    }
}
