using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Hogares.Exceptions;
using Nido.Api.Errors;

namespace Nido.Api.IntegrationTests.Errors;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandle_MapsAccountLinkRequiredException_To409WithCode()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new AccountLinkRequiredException("ACCOUNT_EXISTS_WITH_GOOGLE", "This account was created with Google.");
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandle_MapsInvalidCredentialsException_To401()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidCredentialsException();
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandle_MapsNonNidoException_To500AndLogs()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Something broke");
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandle_MapsNotSoleOwnerException_To409Conflict()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new NotSoleOwnerException();
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandle_MapsNoHouseholdAssociatedException_To500()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new NoHouseholdAssociatedException();
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandle_MapsUserNotFoundException_To404()
    {
        var handler = new ApiExceptionHandler(new FakeProblemDetailsService(), NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new UserNotFoundException();
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(404, context.Response.StatusCode);
    }

    private sealed class FakeProblemDetailsService : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            return ValueTask.CompletedTask;
        }

        public bool TryWriteAsync(ProblemDetailsContext context)
        {
            return true;
        }
    }
}
