using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Auth;

namespace Nido.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = MapException(httpContext, exception);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }

    private static (int Status, string Title) MapException(HttpContext httpContext, Exception exception)
    {
        if (exception is ArgumentException)
            return (StatusCodes.Status400BadRequest, "Validation error");

        if (exception is AccountLinkRequiredException accountLink)
        {
            return (StatusCodes.Status409Conflict, accountLink.Code);
        }

        if (exception is UnauthorizedAccessException
            && httpContext.Request.Path.StartsWithSegments("/onboarding", StringComparison.OrdinalIgnoreCase))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden");
        }

        if (exception is UnauthorizedAccessException)
        {
            return (StatusCodes.Status401Unauthorized, exception.Message);
        }

        if (exception is InvalidOperationException
            && (httpContext.Request.Path.StartsWithSegments("/auth/register", StringComparison.OrdinalIgnoreCase)
                || httpContext.Request.Path.StartsWithSegments("/auth/link-google", StringComparison.OrdinalIgnoreCase)))
        {
            return (StatusCodes.Status409Conflict, "Conflict");
        }

        if (exception is KeyNotFoundException)
        {
            return (StatusCodes.Status404NotFound, "Not found");
        }

        return (StatusCodes.Status500InternalServerError, "Server error");
    }
}
