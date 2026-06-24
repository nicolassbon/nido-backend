using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;
using Nido.Application.Tests.Common.Security;

namespace Nido.Application.Tests.Hogares;

public sealed class GetMiembrosHandlerTests
{
    [Fact]
    public async Task Handle_UserWithoutMembershipInRequestedHousehold_ThrowsNotHouseholdMember()
    {
        var repo = new FakeInvitacionRepository();
        var membershipService = new RecordingHouseholdMembershipService
        {
            MemberExceptionToThrow = new NotHouseholdMemberException()
        };
        var handler = new GetMiembrosHandler(repo, membershipService);
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            handler.Handle(new GetMiembrosQuery(usuarioId, hogarId), CancellationToken.None));

        Assert.Contains((usuarioId, hogarId), membershipService.MemberChecks);
        Assert.Empty(membershipService.AnyMembershipChecks);
    }

    [Fact]
    public async Task Handle_HouseholdMember_ReturnsRepositoryMembers()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var expected = new List<MiembroInfo>
        {
            new(Guid.NewGuid(), "Ana", "ana@test.com", "owner", null, ["Gluten"])
        };
        var repo = new FakeInvitacionRepository
        {
            Miembros = expected
        };
        var membershipService = new RecordingHouseholdMembershipService();
        var handler = new GetMiembrosHandler(repo, membershipService);

        var result = await handler.Handle(new GetMiembrosQuery(usuarioId, hogarId), CancellationToken.None);

        var miembro = Assert.Single(result);
        Assert.Equal(expected[0].UsuarioId, miembro.UsuarioId);
        Assert.Contains("Gluten", miembro.Alergias);
        Assert.Contains((usuarioId, hogarId), membershipService.MemberChecks);
        Assert.Empty(membershipService.AnyMembershipChecks);
    }

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public List<MiembroInfo> Miembros { get; set; } = [];

        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(Miembros);

        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct) => Task.FromResult("token");
        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct) => Task.FromResult<InvitacionInfo?>(null);
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task AddUserToHouseholdAsync(Guid usuarioId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(("user@test.com", "User"));
        public Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct) => Task.CompletedTask;
    }
}
