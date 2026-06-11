using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Finanzas;
using Nido.Application.Common.Security;
using Nido.Application.Finanzas;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/finanzas")]
public sealed class FinanzasController : ControllerBase
{
    private readonly CreateGastoHandler _createGastoHandler;
    private readonly GetGastosHandler _getGastosHandler;
    private readonly GetBalanceHandler _getBalanceHandler;
    private readonly ToggleModoAhorroHandler _toggleModoAhorroHandler;
    private readonly GetRecomendacionesHandler _getRecomendacionesHandler;
    private readonly CreateFacturaHandler _createFacturaHandler;
    private readonly GetFacturasHandler _getFacturasHandler;
    private readonly MarcarPagadaHandler _marcarPagadaHandler;
    private readonly DeleteFacturaHandler _deleteFacturaHandler;

    public FinanzasController(
        CreateGastoHandler createGastoHandler,
        GetGastosHandler getGastosHandler,
        GetBalanceHandler getBalanceHandler,
        ToggleModoAhorroHandler toggleModoAhorroHandler,
        GetRecomendacionesHandler getRecomendacionesHandler,
        CreateFacturaHandler createFacturaHandler,
        GetFacturasHandler getFacturasHandler,
        MarcarPagadaHandler marcarPagadaHandler,
        DeleteFacturaHandler deleteFacturaHandler)
    {
        _createGastoHandler = createGastoHandler;
        _getGastosHandler = getGastosHandler;
        _getBalanceHandler = getBalanceHandler;
        _toggleModoAhorroHandler = toggleModoAhorroHandler;
        _getRecomendacionesHandler = getRecomendacionesHandler;
        _createFacturaHandler = createFacturaHandler;
        _getFacturasHandler = getFacturasHandler;
        _marcarPagadaHandler = marcarPagadaHandler;
        _deleteFacturaHandler = deleteFacturaHandler;
    }

    [HttpGet("modo-ahorro")]
    public async Task<IActionResult> GetModoAhorro(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var activo = await _toggleModoAhorroHandler.GetModoAhorro(currentUser.HogarId, ct);
        if (activo is null) return NotFound();
        return Ok(new ModoAhorroResponse(activo.Value));
    }

    [HttpPatch("modo-ahorro")]
    public async Task<IActionResult> ToggleModoAhorro(
        [FromBody] ModoAhorroRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var activo = await _toggleModoAhorroHandler.Handle(
            new ToggleModoAhorroCommand(currentUser.HogarId, request.Activo),
            ct);

        if (!activo) return NotFound();

        return Ok(new ModoAhorroResponse(request.Activo));
    }

    [HttpGet("recomendaciones")]
    public async Task<IActionResult> GetRecomendaciones(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getRecomendacionesHandler.Handle(currentUser.HogarId, ct);

        return Ok(new RecomendacionesResponse(
            result.Recetas.Select(r => new RecetaRecomendadaResponse(
                r.Id,
                r.Nombre,
                r.ImagenUrl,
                r.IngredientesEnStock,
                r.TotalIngredientes)).ToList(),
            result.Tips));
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getBalanceHandler.Handle(
            new GetBalanceQuery(currentUser.HogarId, desde, hasta),
            ct);

        return Ok(new BalanceResponse(
            result.Miembros.Select(m => new BalanceMiembroResponse(
                m.UsuarioId,
                m.Nombre,
                m.FotoUrl,
                m.MontoAportado,
                m.MontoCorrespondido,
                m.Balance)).ToList(),
            result.TotalPeriodo));
    }

    [HttpGet("gastos")]
    public async Task<IActionResult> GetGastos(
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] string? categoria,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getGastosHandler.Handle(
            new GetGastosQuery(currentUser.HogarId, desde, hasta, categoria),
            ct);

        return Ok(new GastosListResponse(
            result.Gastos.Select(ToResponse).ToList(),
            result.TotalPeriodo));
    }

    [HttpPost("gastos")]
    public async Task<IActionResult> CreateGasto(
        [FromBody] CreateGastoRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var pagadoPorId = request.PagadoPorId ?? currentUser.UsuarioId;

        var result = await _createGastoHandler.Handle(
            new CreateGastoCommand(
                currentUser.HogarId,
                pagadoPorId,
                request.Monto,
                request.Descripcion,
                request.Categoria,
                request.Fecha),
            ct);

        return CreatedAtAction(nameof(CreateGasto), ToResponse(result));
    }

    [HttpGet("facturas")]
    public async Task<IActionResult> GetFacturas(
        [FromQuery] string? tipo,
        [FromQuery] bool? pagada,
        [FromQuery] int? proximaDias,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var facturas = await _getFacturasHandler.Handle(
            new GetFacturasQuery(currentUser.HogarId, tipo, pagada, proximaDias),
            ct);

        return Ok(facturas.Select(ToFacturaResponse));
    }

    [HttpPost("facturas")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateFactura(
        [FromForm] CreateFacturaRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        FacturaFileUpload? archivo = null;
        if (request.Archivo is not null)
        {
            await using var ms = new MemoryStream();
            await request.Archivo.CopyToAsync(ms, ct);
            archivo = new FacturaFileUpload(request.Archivo.FileName, request.Archivo.ContentType, ms.ToArray());
        }

        var result = await _createFacturaHandler.Handle(
            new CreateFacturaCommand(
                currentUser.HogarId,
                currentUser.UsuarioId,
                request.Nombre,
                request.Tipo,
                request.Monto,
                request.FechaVencimiento,
                archivo),
            ct);

        return CreatedAtAction(nameof(GetFacturas), ToFacturaResponse(result));
    }

    [HttpPatch("facturas/{id:guid}/pagar")]
    public async Task<IActionResult> MarcarPagada(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _marcarPagadaHandler.Handle(id, currentUser.HogarId, currentUser.UsuarioId, ct);
        return Ok(new PagarFacturaResponse(
            ToFacturaResponse(result.Factura),
            result.GastoCreado is not null ? ToResponse(result.GastoCreado) : null));
    }

    [HttpDelete("facturas/{id:guid}")]
    public async Task<IActionResult> DeleteFactura(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await _deleteFacturaHandler.Handle(id, currentUser.HogarId, ct);
        return NoContent();
    }

    private static FacturaResponse ToFacturaResponse(FacturaResult result) => new(
        result.Id,
        result.Nombre,
        result.Tipo,
        result.Monto,
        result.FechaVencimiento,
        result.ArchivoUrl,
        result.Pagada,
        result.DiasParaVencer,
        result.CreadoPor,
        result.CreadoPorNombre,
        result.CreatedAt
    );

    private static GastoResponse ToResponse(GastoResult result) => new(
        result.Id,
        result.Monto,
        result.Descripcion,
        result.Categoria,
        result.Fecha,
        result.PagadoPorId,
        result.PagadoPorNombre,
        result.CreatedAt
    );
}
