using System.Net;
using Nido.Application.Productos;
using Nido.Application.Productos.Exceptions;
using Nido.Infrastructure.Productos;

namespace Nido.Infrastructure.Tests.Productos;

public sealed class PriceComparatorServiceTests
{
    [Fact]
    public async Task CompareAsync_WhenComparatorReturnsSuccess_ReturnsDeserializedResult()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "products": [
                    {
                      "id": "1",
                      "source": "test",
                      "name": "Leche",
                      "link": "https://example.test/leche",
                      "image": "https://example.test/leche.png",
                      "price": 1234.5,
                      "unit": "1 l",
                      "unitPrice": 1234.5
                    }
                  ],
                  "failedScrapers": [],
                  "timestamp": "2026-07-06T12:00:00Z"
                }
                """)
            }))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        var result = await service.CompareAsync("leche", CancellationToken.None);

        var product = Assert.Single(result.Products);
        Assert.Equal("Leche", product.Name);
    }

    [Fact]
    public async Task CompareAsync_WhenComparatorIsUnavailable_ThrowsFunctionalException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("connection refused")))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        var exception = await Assert.ThrowsAsync<ComparatorUnavailableException>(() =>
            service.CompareAsync("leche", CancellationToken.None));

        Assert.Equal("No pudimos comparar precios en este momento. Intentá nuevamente en unos minutos.", exception.Message);
    }

    [Fact]
    public async Task CompareAsync_WhenComparatorReturnsServerError_ThrowsFunctionalException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway)))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        await Assert.ThrowsAsync<ComparatorUnavailableException>(() =>
            service.CompareAsync("leche", CancellationToken.None));
    }

    [Fact]
    public async Task CompareAsync_WhenComparatorReturnsInvalidJson_ThrowsFunctionalException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json")
            }))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        await Assert.ThrowsAsync<ComparatorUnavailableException>(() =>
            service.CompareAsync("leche", CancellationToken.None));
    }

    [Fact]
    public async Task CompareAsync_WhenComparatorTimesOut_ThrowsFunctionalException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new OperationCanceledException("dependency timeout")))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        await Assert.ThrowsAsync<ComparatorUnavailableException>(() =>
            service.CompareAsync("leche", CancellationToken.None));
    }

    [Fact]
    public async Task CompareAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new OperationCanceledException(cts.Token)))
        {
            BaseAddress = new Uri("https://comparator.test/")
        };
        var service = new PriceComparatorService(httpClient);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CompareAsync("leche", cts.Token));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
