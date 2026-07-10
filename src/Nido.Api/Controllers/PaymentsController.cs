using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Common.Security;
using Nido.Application.Payments;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly CreateCheckoutPreferenceHandler _createCheckoutPreferenceHandler;
    private readonly GetSubscriptionHandler _getSubscriptionHandler;

    public PaymentsController(
        CreateCheckoutPreferenceHandler createCheckoutPreferenceHandler,
        GetSubscriptionHandler getSubscriptionHandler)
    {
        _createCheckoutPreferenceHandler = createCheckoutPreferenceHandler;
        _getSubscriptionHandler = getSubscriptionHandler;
    }

    [HttpPost("checkout")]
    [HttpPost("preferences")]
    public async Task<IActionResult> CreatePreference(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _createCheckoutPreferenceHandler.Handle(
            new CreateCheckoutPreferenceCommand(currentUser.HogarId),
            ct);

        return Ok(new CheckoutPreferenceResponse(result.PreferenceId, result.InitPoint));
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var result = await _getSubscriptionHandler.Handle(currentUser.HogarId, ct);
        return Ok(new SubscriptionResponse(result.Plan, result.SubscriptionStatus, result.TrialEndsAt, result.SubscriptionEndsAt));
    }

    private sealed record CheckoutPreferenceResponse(string PreferenceId, string InitPoint);

    private sealed record SubscriptionResponse(string Plan, string SubscriptionStatus, DateTime? TrialEndsAt, DateTime? SubscriptionEndsAt);
}
