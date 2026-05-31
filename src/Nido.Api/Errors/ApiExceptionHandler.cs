using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nido.Application.Alacena.Exceptions;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Electrodomesticos.Exceptions;
using Nido.Application.Hogares.Exceptions;
using Nido.Application.Onboarding.Exceptions;
using Nido.Application.Preferencias.Exceptions;
using Nido.Application.Productos.Exceptions;
using Nido.Domain.Exceptions;

namespace Nido.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = MapException(exception);

        if (exception is NidoException nidoEx)
        {
            title = nidoEx.Code;
        }
        else if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {ExceptionType}", exception.GetType().Name);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError 
                ? "An unexpected error occurred." 
                : exception.Message
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

    private static (int Status, string Title) MapException(Exception exception) => exception switch
    {
        // Auth exceptions
        LoginCredentialsMissingException => (StatusCodes.Status400BadRequest, "Validation error"),
        MissingRegistrationFieldsException => (StatusCodes.Status400BadRequest, "Validation error"),
        WeakPasswordException => (StatusCodes.Status400BadRequest, "Validation error"),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        InvalidGoogleTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        EmailAlreadyExistsException => (StatusCodes.Status409Conflict, "Conflict"),
        AccountAlreadyLinkedException => (StatusCodes.Status409Conflict, "Conflict"),
        AccountLinkRequiredException => (StatusCodes.Status409Conflict, "Conflict"),
        UserNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        UserNotInHouseholdException => (StatusCodes.Status404NotFound, "Not found"),

        // Onboarding exceptions
        BoundaryViolationException => (StatusCodes.Status403Forbidden, "Forbidden"),
        HouseholdAccessDeniedException => (StatusCodes.Status403Forbidden, "Forbidden"),

        // Electrodomesticos exceptions
        MissingApplianceFieldException => (StatusCodes.Status400BadRequest, "Validation error"),
        HouseholdNotFoundException => (StatusCodes.Status404NotFound, "Not found"),

        // Hogares exceptions
        MissingInvitationTokenException => (StatusCodes.Status400BadRequest, "Validation error"),
        InvitationNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        InvitationAlreadyProcessedException => (StatusCodes.Status409Conflict, "Conflict"),
        InvitationExpiredException => (StatusCodes.Status410Gone, "Gone"),
        MaxMembersExceededException => (StatusCodes.Status409Conflict, "Conflict"),
        AlreadyMemberException => (StatusCodes.Status409Conflict, "Conflict"),
        NotSoleOwnerException => (StatusCodes.Status409Conflict, "Conflict"),
        NotHouseholdOwnerException => (StatusCodes.Status403Forbidden, "Forbidden"),
        NotHouseholdMemberException => (StatusCodes.Status403Forbidden, "Forbidden"),

        // Alacena exceptions
        InvalidStockItemDateException => (StatusCodes.Status400BadRequest, "Validation error"),
        MissingStockItemFieldException => (StatusCodes.Status400BadRequest, "Validation error"),

        // Productos exceptions
        MissingProductFieldException => (StatusCodes.Status400BadRequest, "Validation error"),

        // Preferencias exceptions
        MissingPreferenceFieldException => (StatusCodes.Status400BadRequest, "Validation error"),
        InvalidPreferenceRangeException => (StatusCodes.Status400BadRequest, "Validation error"),

        // Catch-all for remaining infrastructure-level validation errors
        ArgumentException => (StatusCodes.Status400BadRequest, "Validation error"),

        _ => (StatusCodes.Status500InternalServerError, "Server error")
    };
}
