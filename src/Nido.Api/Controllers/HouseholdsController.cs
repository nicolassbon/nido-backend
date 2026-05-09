using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Households;
using Nido.Application.Households;

namespace Nido.Api.Controllers;

[ApiController]
[Route("household")]
public sealed class HouseholdsController : ControllerBase
{
    private readonly CreateHouseholdHandler _handler;

    public HouseholdsController(CreateHouseholdHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateHouseholdRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Problem(
                title: "Invalid household name",
                detail: "Name is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await _handler.Handle(new CreateHouseholdCommand(request.Name), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new HouseholdResponse(result.Id, result.Name));
        }
        catch (ArgumentException)
        {
            return Problem(
                title: "Invalid household name",
                detail: "Name is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
