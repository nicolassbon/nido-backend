using Nido.Application.Productos;
using Nido.Application.Payments;
using Nido.Application.Payments.Exceptions;

namespace Nido.Application.Tests.Productos;

public sealed class ComparePricesHandlerTests
{
    [Fact]
    public async Task Handle_WhenQueryHasWhitespaceAndUppercase_NormalizesBeforeCallingComparator()
    {
        var comparator = new FakePriceComparatorService();
        var entitlement = new FakeEntitlementService(HouseholdPlan.Premium);
        var hogarId = Guid.NewGuid();
        var handler = new ComparePricesHandler(comparator, entitlement);

        await handler.Handle(new ComparePricesQuery("  LeChe Entera  ", hogarId), CancellationToken.None);

        Assert.Equal("leche entera", comparator.QueryReceived);
        Assert.Equal(hogarId, entitlement.HogarChecked);
    }

    [Fact]
    public async Task Handle_WhenQueryIsBlank_ThrowsArgumentException()
    {
        var handler = new ComparePricesHandler(new FakePriceComparatorService(), new FakeEntitlementService(HouseholdPlan.Premium));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new ComparePricesQuery("  ", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenHouseholdIsFree_ThrowsPremiumRequiredBeforeCallingComparator()
    {
        var comparator = new FakePriceComparatorService();
        var handler = new ComparePricesHandler(comparator, new FakeEntitlementService(HouseholdPlan.Free));

        await Assert.ThrowsAsync<PremiumRequiredException>(() =>
            handler.Handle(new ComparePricesQuery("leche", Guid.NewGuid()), CancellationToken.None));

        Assert.Null(comparator.QueryReceived);
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

    private sealed class FakeEntitlementService : IEntitlementService
    {
        private readonly HouseholdPlan _plan;

        public FakeEntitlementService(HouseholdPlan plan)
        {
            _plan = plan;
        }

        public Guid? HogarChecked { get; private set; }

        public Task EnsurePremiumAsync(Guid hogarId, CancellationToken ct)
        {
            HogarChecked = hogarId;
            if (_plan == HouseholdPlan.Premium)
            {
                return Task.CompletedTask;
            }

            throw new PremiumRequiredException();
        }

        public Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct) =>
            Task.FromResult(new HouseholdEntitlement(_plan, SubscriptionStatus.None, null));
    }
}
