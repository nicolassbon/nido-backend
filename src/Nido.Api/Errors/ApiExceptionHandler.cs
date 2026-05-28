using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
        {
            return (StatusCodes.Status400BadRequest, "Validation error");
        }

        if (exception is UnauthorizedAccessException && httpContext.Request.Path.StartsWithSegments("/onboarding", StringComparison.OrdinalIgnoreCase))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden");
        }

        if (exception is InvalidOperationException && httpContext.Request.Path.StartsWithSegments("/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return (StatusCodes.Status409Conflict, "Conflict");
        }

        return (StatusCodes.Status500InternalServerError, "Server error");
    }
}
