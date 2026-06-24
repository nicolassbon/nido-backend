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
    private readonly CreateFacturaHandler _createFacturaHandler;
    private readonly GetFacturasHandler _getFacturasHandler;
    private readonly MarcarPagadaHandler _marcarPagadaHandler;
    private readonly DeleteFacturaHandler _deleteFacturaHandler;
    private readonly GetSavingsPotencialHandler _getSavingsPotencialHandler;
    private readonly GetInsightsHandler _getInsightsHandler;
    private readonly GetAlacenaOportunidadesHandler _getAlacenaOportunidadesHandler;
    private readonly GetPresupuestoHandler _getPresupuestoHandler;
    private readonly SetPresupuestoHandler _setPresupuestoHandler;
    private readonly UpdateGastoHandler _updateGastoHandler;
    private readonly DeleteGastoHandler _deleteGastoHandler;

    public FinanzasController(
        CreateGastoHandler createGastoHandler,
        GetGastosHandler getGastosHandler,
        GetBalanceHandler getBalanceHandler,
        ToggleModoAhorroHandler toggleModoAhorroHandler,
        CreateFacturaHandler createFacturaHandler,
        GetFacturasHandler getFacturasHandler,
        MarcarPagadaHandler marcarPagadaHandler,
        DeleteFacturaHandler deleteFacturaHandler,
        GetSavingsPotencialHandler getSavingsPotencialHandler,
        GetInsightsHandler getInsightsHandler,
        GetAlacenaOportunidadesHandler getAlacenaOportunidadesHandler,
        GetPresupuestoHandler getPresupuestoHandler,
        SetPresupuestoHandler setPresupuestoHandler,
        UpdateGastoHandler updateGastoHandler,
        DeleteGastoHandler deleteGastoHandler)
    {
        _createGastoHandler = createGastoHandler;
        _getGastosHandler = getGastosHandler;
        _getBalanceHandler = getBalanceHandler;
        _toggleModoAhorroHandler = toggleModoAhorroHandler;
        _createFacturaHandler = createFacturaHandler;
        _getFacturasHandler = getFacturasHandler;
        _marcarPagadaHandler = marcarPagadaHandler;
        _deleteFacturaHandler = deleteFacturaHandler;
        _getSavingsPotencialHandler = getSavingsPotencialHandler;
        _getInsightsHandler = getInsightsHandler;
        _getAlacenaOportunidadesHandler = getAlacenaOportunidadesHandler;
        _getPresupuestoHandler = getPresupuestoHandler;
        _setPresupuestoHandler = setPresupuestoHandler;
        _updateGastoHandler = updateGastoHandler;
        _deleteGastoHandler = deleteGastoHandler;
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

    [HttpGet("modo-ahorro/potencial")]
    public async Task<IActionResult> GetSavingsPotencial(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getSavingsPotencialHandler.Handle(currentUser.HogarId, ct);
        return Ok(new SavingsPotencialResponse(
            result.TotalPotencial,
            result.PeriodoBaseMeses,
            result.Categorias.Select(c => new SavingsCategoriaResponse(
                c.Nombre, c.GastoActual, c.MediaHistorica, c.PotencialAhorro, c.VariacionPct, c.Color)).ToList()));
    }

    [HttpGet("modo-ahorro/insights")]
    public async Task<IActionResult> GetInsights(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getInsightsHandler.Handle(currentUser.HogarId, ct);
        return Ok(new InsightsListResponse(
            result.Insights.Select(i => new InsightResponse(
                i.Tipo, i.Categoria, i.Titulo, i.Detalle, i.Accion, i.Emoji, i.VariacionPct, i.TendenciaMeses)).ToList()));
    }

    [HttpGet("modo-ahorro/alacena")]
    public async Task<IActionResult> GetAlacenaOportunidades(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getAlacenaOportunidadesHandler.Handle(currentUser.HogarId, ct);
        return Ok(new AlacenaOportunidadesResponse(
            result.ProductosDestacados.Select(p => new ProductoDestacadoResponse(
                p.Id, p.Nombre, p.Ubicacion, p.DiasParaVencer, p.Imagen, p.PorcentajeConsumido)).ToList(),
            result.RecetasSugeridas.Select(r => new RecetaRecomendadaResponse(
                r.Id, r.Nombre, r.ImagenUrl, r.IngredientesEnStock, r.TotalIngredientes)).ToList(),
            result.TotalProductosEnCasa));
    }

    [HttpGet("presupuesto")]
    public async Task<IActionResult> GetPresupuesto(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getPresupuestoHandler.Handle(currentUser.HogarId, ct);
        return Ok(new PresupuestoResponse(result.Monto, result.GastoActual, result.Restante, result.Anio, result.Mes));
    }

    [HttpPut("presupuesto")]
    public async Task<IActionResult> SetPresupuesto(
        [FromBody] SetPresupuestoRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var result = await _setPresupuestoHandler.Handle(
            new SetPresupuestoCommand(currentUser.HogarId, request.Monto, now.Year, now.Month),
            ct);
        return Ok(new PresupuestoResponse(result.Monto, result.GastoActual, result.Restante, result.Anio, result.Mes));
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

    [HttpPatch("gastos/{id:guid}")]
    public async Task<IActionResult> UpdateGasto(
        Guid id,
        [FromBody] UpdateGastoRequest request,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var pagadoPorId = request.PagadoPorId ?? currentUser.UsuarioId;

        var result = await _updateGastoHandler.Handle(
            new UpdateGastoCommand(id, currentUser.HogarId, request.Monto, request.Descripcion, request.Categoria, request.Fecha, pagadoPorId),
            ct);

        return Ok(ToResponse(result));
    }

    [HttpDelete("gastos/{id:guid}")]
    public async Task<IActionResult> DeleteGasto(
        Guid id,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _deleteGastoHandler.Handle(id, currentUser.HogarId, ct);
        return Ok(new DeleteGastoResponse(result.FacturaRevertida, result.FacturaId));
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
        result.CreatedAt,
        result.FacturaId
    );
}
