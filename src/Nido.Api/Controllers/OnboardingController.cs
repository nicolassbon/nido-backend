using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Onboarding;
using Nido.Application.Common.Security;
using Nido.Application.Onboarding;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    [HttpGet("preferencias-alimentarias")]
    public async Task<IActionResult> GetPreferenciasAlimentarias(
        [FromServices] GetPreferenciasAlimentariasHandler handler,
        CancellationToken cancellationToken)
    {
        var results = await handler.Handle(cancellationToken);
        return Ok(results.Select(x => new RestriccionCatalogoResponse(x.Id, x.Nombre, x.Tipo)));
    }

    [HttpGet("alergias")]
    public async Task<IActionResult> GetAlergias(
        [FromQuery] string? q,
        [FromServices] GetAlergiasHandler handler,
        CancellationToken cancellationToken)
    {
        var results = await handler.Handle(q, cancellationToken);
        return Ok(results.Select(x => new RestriccionCatalogoResponse(x.Id, x.Nombre, x.Tipo)));
    }

    [HttpGet("metas")]
    public async Task<IActionResult> GetMetas(
        [FromServices] GetMetasHandler handler,
        CancellationToken cancellationToken)
    {
        var results = await handler.Handle(cancellationToken);
        return Ok(results.Select(x => new MetaCatalogoResponse(x.Id, x.Nombre)));
    }

    [HttpPatch("step-2")]
    public async Task<IActionResult> SaveHousehold(
        [FromBody] HouseholdOnboardingRequest request,
        [FromServices] SaveHouseholdStepHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new SaveHouseholdStepCommand(
            currentUser.UsuarioId,
            currentUser.HogarId,
            request.Skip,
            (request.Members ?? []).Select(x => new RepresentedMemberInput(x.Nombre, x.Rol)).ToList(),
            request.UsuarioId,
            request.HogarId), cancellationToken);

        return NoContent();
    }

    [HttpPatch("step-3")]
    public async Task<IActionResult> SaveEquipment([FromBody] EquipmentOnboardingRequest request,
        [FromServices] SaveEquipmentStepHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new SaveEquipmentStepCommand(
            currentUser.UsuarioId,
            currentUser.HogarId,
            request.Skip,
            (request.Equipments ?? []).Select(x => new EquipmentInput(x.CatalogoId, x.Nombre, x.Tipo, x.Estado)).ToList(),
            request.UsuarioId,
            request.HogarId), cancellationToken);

        return NoContent();
    }

    [HttpPatch("step-4")]
    public async Task<IActionResult> SaveWellness([FromBody] WellnessOnboardingRequest request,
        [FromServices] SaveWellnessStepHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new SaveWellnessStepCommand(
            currentUser.UsuarioId,
            currentUser.HogarId,
            request.Skip,
            request.RestriccionIds ?? [],
            request.MetaIds ?? [],
            request.UsuarioId,
            request.HogarId), cancellationToken);

        return NoContent();
    }

    [HttpGet("wellness")]
    public async Task<IActionResult> GetWellness(
        [FromServices] IOnboardingRepository repository,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var restriccionIds = await repository.GetUserRestriccionesAsync(currentUser.UsuarioId, cancellationToken);
        var metaIds = await repository.GetHogarMetasAsync(currentUser.HogarId, cancellationToken);
        return Ok(new WellnessOnboardingResponse(restriccionIds, metaIds));
    }
}
