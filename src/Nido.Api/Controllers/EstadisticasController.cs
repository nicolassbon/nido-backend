using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Estadisticas;
using Nido.Application.Common.Security;
using Nido.Application.Estadisticas;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/estadisticas")]
public sealed class EstadisticasController : ControllerBase
{
    private readonly GetEstadisticasConsumoHandler _getConsumoHandler;

    public EstadisticasController(GetEstadisticasConsumoHandler getConsumoHandler)
    {
        _getConsumoHandler = getConsumoHandler;
    }

    [HttpGet("consumo")]
    public async Task<IActionResult> GetConsumo(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getConsumoHandler.Handle(
            new GetEstadisticasConsumoQuery(currentUser.HogarId), ct);

        return Ok(new EstadisticasConsumoResponse(
            result.Total,
            result.TotalAbiertos,
            result.TotalPorVencer,
            result.TotalParaReponer,
            result.Items.Select(i => new ProductoConsumoResponse(
                i.StockId,
                i.ProductoId,
                i.Nombre,
                i.CategoriaNombre,
                i.Ubicacion,
                i.CantidadActual,
                i.UnidadMedida,
                i.EstaAbierto,
                i.PorcentajeConsumido,
                i.FechaVencimiento,
                i.DiasHastaVencimiento,
                i.TasaConsumoDiariaEstimada,
                i.DiasHastaAgotamientoEstimado,
                i.PrioridadReposicion)).ToList()));
    }
}
