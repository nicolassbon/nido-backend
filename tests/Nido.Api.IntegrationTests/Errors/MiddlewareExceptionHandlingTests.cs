using System.Net;
using System.Net.Http.Json;
using Nido.Api.IntegrationTests;
using Nido.Application.Auth.Exceptions;

namespace Nido.Api.IntegrationTests.Errors;

public sealed class MiddlewareExceptionHandlingTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public MiddlewareExceptionHandlingTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExceptionThrownAfterExceptionHandler_IsCaughtAndReturnsSafe500ProblemDetails()
    {
        var factory = _factory.WithAfterAppConfiguration(app =>
        {
            app.Use(next => context =>
            {
                if (context.Request.Path.StartsWithSegments("/test-throw"))
                {
                    throw new InvalidOperationException("Test exception — should NOT leak to client");
                }

                return next(context);
            });
        });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails!.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
        Assert.DoesNotContain("Test exception", problemDetails.Detail);
        Assert.DoesNotContain("stack", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Nido", problemDetails.Title ?? "");
    }

    [Fact]
    public async Task NidoExceptionThrownAfterExceptionHandler_IsCaughtAndMappedCorrectly()
    {
        var factory = _factory.WithAfterAppConfiguration(app =>
        {
            app.Use(next => context =>
            {
                if (context.Request.Path.StartsWithSegments("/test-throw-nido"))
                {
                    throw new UserNotFoundException();
                }

                return next(context);
            });
        });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-throw-nido");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails!.Status);
        Assert.Equal("USER_NOT_FOUND", problemDetails.Title);
        Assert.Equal("User not found.", problemDetails.Detail);
    }

    private sealed record ProblemDetailsPayload
    {
        public int? Status { get; init; }
        public string? Title { get; init; }
        public string? Detail { get; init; }
    }
}
