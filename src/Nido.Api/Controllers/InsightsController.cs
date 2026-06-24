using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Common.Security;
using Nido.Application.Insights;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/insights")]
public sealed class InsightsController : ControllerBase
{
    private readonly GetInsightsHogarHandler _handler;

    public InsightsController(GetInsightsHogarHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("hogar")]
    public async Task<IActionResult> GetForHogar(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _handler.Handle(new GetInsightsHogarQuery(currentUser.HogarId), ct);
        return Ok(result);
    }
}
