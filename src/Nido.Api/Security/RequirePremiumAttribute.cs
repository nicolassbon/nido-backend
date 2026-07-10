using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nido.Application.Common.Security;
using Nido.Application.Payments;
using Nido.Application.Payments.Exceptions;

namespace Nido.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePremiumAttribute : TypeFilterAttribute
{
    public RequirePremiumAttribute() : base(typeof(RequirePremiumFilter))
    {
    }
}

public sealed class RequirePremiumFilter : IAsyncAuthorizationFilter
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IEntitlementService _entitlementService;

    public RequirePremiumFilter(
        ICurrentUserContext currentUser,
        IEntitlementService entitlementService)
    {
        _currentUser = currentUser;
        _entitlementService = entitlementService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            await _entitlementService.EnsurePremiumAsync(_currentUser.HogarId, context.HttpContext.RequestAborted);
        }
        catch (PremiumRequiredException ex)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = ex.Code,
                Detail = ex.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
