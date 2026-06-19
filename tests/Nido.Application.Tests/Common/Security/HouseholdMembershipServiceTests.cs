using Nido.Application.Common.Security;
using Nido.Application.Hogares.Exceptions;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Tests.Common.Security;

public sealed class HouseholdMembershipServiceTests
{
    [Fact]
    public async Task EnsureOwnerAsync_WhenUserIsNotOwner_ThrowsNotHouseholdOwnerException()
    {
        var repository = new FakeHogarMembershipRepository { IsOwner = false };
        var service = new HouseholdMembershipService(repository);

        await Assert.ThrowsAsync<NotHouseholdOwnerException>(() =>
            service.EnsureOwnerAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EnsureMemberAsync_WhenUserIsNotMember_ThrowsNotHouseholdMemberException()
    {
        var repository = new FakeHogarMembershipRepository { IsMember = false };
        var service = new HouseholdMembershipService(repository);

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            service.EnsureMemberAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EnsureMemberAsync_WhenCustomFactoryIsProvided_ThrowsFactoryException()
    {
        var repository = new FakeHogarMembershipRepository { IsMember = false };
        var service = new HouseholdMembershipService(repository);

        await Assert.ThrowsAsync<HouseholdAccessDeniedException>(() =>
            service.EnsureMemberAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                static () => new HouseholdAccessDeniedException(),
                CancellationToken.None));
    }

    [Fact]
    public async Task EnsureAnyMembershipAsync_WhenUserIsNotInAnyHousehold_ThrowsNotHouseholdMemberException()
    {
        var repository = new FakeHogarMembershipRepository { IsInAnyHousehold = false };
        var service = new HouseholdMembershipService(repository);

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            service.EnsureAnyMembershipAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class FakeHogarMembershipRepository : IHogarMembershipRepository
    {
        public bool IsOwner { get; set; } = true;
        public bool IsMember { get; set; } = true;
        public bool IsInAnyHousehold { get; set; } = true;

        public Task<bool> IsOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(IsOwner);
        public Task<bool> IsMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(IsMember);
        public Task<bool> IsInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(IsInAnyHousehold);
    }
}
