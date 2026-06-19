using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;
using Nido.Application.Tests.Common.Security;

namespace Nido.Application.Tests.Hogares;

public sealed class RemoveMiembroHandlerTests
{
    [Fact]
    public async Task Handle_OwnerRemovingAnotherMember_RemovesTarget()
    {
        var repo = new FakeInvitacionRepository();
        var membershipService = new RecordingHouseholdMembershipService();
        var command = new RemoveMiembroCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new RemoveMiembroHandler(repo, membershipService);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.HogarId, repo.RemovedHogarId);
        Assert.Equal(command.TargetUsuarioId, repo.RemovedUsuarioId);
        Assert.Single(membershipService.OwnerChecks);
        Assert.Single(membershipService.MemberChecks);
    }

    [Fact]
    public async Task Handle_CallerIsNotOwner_ThrowsNotHouseholdOwner()
    {
        var membershipService = new RecordingHouseholdMembershipService
        {
            OwnerExceptionToThrow = new NotHouseholdOwnerException()
        };
        var handler = new RemoveMiembroHandler(new FakeInvitacionRepository(), membershipService);

        await Assert.ThrowsAsync<NotHouseholdOwnerException>(() =>
            handler.Handle(new RemoveMiembroCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CallerRemovesSelf_ThrowsCannotRemoveSelf()
    {
        var usuarioId = Guid.NewGuid();
        var handler = new RemoveMiembroHandler(new FakeInvitacionRepository(), new RecordingHouseholdMembershipService());

        await Assert.ThrowsAsync<CannotRemoveSelfException>(() =>
            handler.Handle(new RemoveMiembroCommand(usuarioId, Guid.NewGuid(), usuarioId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TargetNotInHousehold_ThrowsNotHouseholdMember()
    {
        var membershipService = new RecordingHouseholdMembershipService();
        membershipService.MemberExceptionToThrow = new NotHouseholdMemberException();
        var handler = new RemoveMiembroHandler(new FakeInvitacionRepository(), membershipService);

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            handler.Handle(new RemoveMiembroCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public Guid? RemovedHogarId { get; private set; }
        public Guid? RemovedUsuarioId { get; private set; }

        public Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct)
        {
            RemovedHogarId = hogarId;
            RemovedUsuarioId = targetUsuarioId;
            return Task.CompletedTask;
        }

        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct) => Task.FromResult("token");
        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct) => Task.FromResult<InvitacionInfo?>(null);
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(Guid.Empty);
        public Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(("user@test.com", "User"));
    }
}
