using Nido.Application.Productos;

namespace Nido.Application.Tests.Productos;

public sealed class ComparePricesHandlerTests
{
    [Fact]
    public async Task Handle_WhenQueryHasWhitespaceAndUppercase_NormalizesBeforeCallingComparator()
    {
        var comparator = new FakePriceComparatorService();
        var handler = new ComparePricesHandler(comparator);

        await handler.Handle(new ComparePricesQuery("  LeChe Entera  "), CancellationToken.None);

        Assert.Equal("leche entera", comparator.QueryReceived);
    }

    [Fact]
    public async Task Handle_WhenQueryIsBlank_ThrowsArgumentException()
    {
        var handler = new ComparePricesHandler(new FakePriceComparatorService());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new ComparePricesQuery("  "), CancellationToken.None));
    }

    private sealed class FakePriceComparatorService : IPriceComparatorService
    {
        public string? QueryReceived { get; private set; }

        public Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct)
        {
            QueryReceived = query;
            return Task.FromResult(new ComparePricesResult(new(), new(), DateTime.UtcNow));
        }
    }
}
