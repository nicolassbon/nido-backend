using Nido.Application.Payments;
using Nido.Application.Payments.Exceptions;

namespace Nido.Application.Tests.Payments;

public sealed class EntitlementServiceTests
{
    [Fact]
    public void HouseholdPlan_HasExpectedValues()
    {
        Assert.Equal(0, (int)HouseholdPlan.Free);
        Assert.Equal(1, (int)HouseholdPlan.Premium);
    }

    [Fact]
    public void SubscriptionStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)SubscriptionStatus.None);
        Assert.Equal(1, (int)SubscriptionStatus.Pending);
        Assert.Equal(2, (int)SubscriptionStatus.Active);
        Assert.Equal(3, (int)SubscriptionStatus.PastDue);
        Assert.Equal(4, (int)SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void HouseholdEntitlement_ExposesPlanStatusAndTrial()
    {
        var entitlement = new HouseholdEntitlement(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(HouseholdPlan.Premium, entitlement.Plan);
        Assert.Equal(SubscriptionStatus.Active, entitlement.SubscriptionStatus);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), entitlement.TrialEndsAt);
    }

    [Fact]
    public async Task EnsurePremiumAsync_FreeHousehold_ThrowsPremiumRequiredException()
    {
        var repository = new FakeEntitlementRepository(HouseholdPlan.Free, SubscriptionStatus.None, null);
        var service = new EntitlementService(repository);

        var ex = await Assert.ThrowsAsync<PremiumRequiredException>(
            () => service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("PLAN_UPGRADE_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task EnsurePremiumAsync_PremiumHousehold_DoesNotThrow()
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            trialEndsAt: null,
            subscriptionEndsAt: DateTime.UtcNow.AddDays(30));
        var service = new EntitlementService(repository);

        await service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Cancelled)]
    public async Task EnsurePremiumAsync_FreeHouseholdWithActiveTrial_IsAllowed(SubscriptionStatus status)
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Free,
            status,
            DateTime.UtcNow.AddDays(7));
        var service = new EntitlementService(repository);

        await service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task EnsurePremiumAsync_FreeHouseholdWithExpiredTrial_Throws()
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Free,
            SubscriptionStatus.None,
            DateTime.UtcNow.AddDays(-1));
        var service = new EntitlementService(repository);

        await Assert.ThrowsAsync<PremiumRequiredException>(
            () => service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_ReturnsEntitlementFromRepository()
    {
        var expectedTrial = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            expectedTrial);
        var service = new EntitlementService(repository);
        var hogarId = Guid.NewGuid();

        var result = await service.GetAsync(hogarId, CancellationToken.None);

        Assert.Equal(HouseholdPlan.Premium, result.Plan);
        Assert.Equal(SubscriptionStatus.Active, result.SubscriptionStatus);
        Assert.Equal(expectedTrial, result.TrialEndsAt);
        Assert.Equal(hogarId, repository.LastRequestedHogarId);
    }

    [Fact]
    public async Task EnsurePremiumAsync_PremiumHouseholdWithActiveSubscription_IsAllowed()
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            trialEndsAt: null,
            subscriptionEndsAt: DateTime.UtcNow.AddDays(15));
        var service = new EntitlementService(repository);

        await service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task EnsurePremiumAsync_PremiumHouseholdWithExpiredSubscription_Throws()
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            trialEndsAt: null,
            subscriptionEndsAt: DateTime.UtcNow.AddDays(-1));
        var service = new EntitlementService(repository);

        await Assert.ThrowsAsync<PremiumRequiredException>(
            () => service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EnsurePremiumAsync_PremiumHouseholdWithNullSubscriptionEndsAtAndNoTrial_Throws()
    {
        var repository = new FakeEntitlementRepository(
            HouseholdPlan.Premium,
            SubscriptionStatus.Active,
            trialEndsAt: null,
            subscriptionEndsAt: null);
        var service = new EntitlementService(repository);

        await Assert.ThrowsAsync<PremiumRequiredException>(
            () => service.EnsurePremiumAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class FakeEntitlementRepository : IEntitlementRepository
    {
        private readonly HouseholdEntitlement _entitlement;

        public FakeEntitlementRepository(HouseholdPlan plan, SubscriptionStatus status, DateTime? trialEndsAt, DateTime? subscriptionEndsAt = null)
        {
            _entitlement = new HouseholdEntitlement(plan, status, trialEndsAt, subscriptionEndsAt);
        }

        public Guid LastRequestedHogarId { get; private set; }

        public Task<HouseholdEntitlement> GetAsync(Guid hogarId, CancellationToken ct)
        {
            LastRequestedHogarId = hogarId;
            return Task.FromResult(_entitlement);
        }
    }
}
