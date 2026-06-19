using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class GetMiembrosHandlerTests
{
    [Fact]
    public async Task Handle_UserWithoutHousehold_ThrowsNotHouseholdMember()
    {
        var repo = new FakeInvitacionRepository { IsUserInAnyHousehold = false };
        var handler = new GetMiembrosHandler(repo);

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            handler.Handle(new GetMiembrosQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HouseholdMember_ReturnsRepositoryMembers()
    {
        var expected = new List<MiembroInfo>
        {
            new(Guid.NewGuid(), "Ana", "ana@test.com", "owner", null, ["Gluten"])
        };
        var repo = new FakeInvitacionRepository
        {
            IsUserInAnyHousehold = true,
            Miembros = expected
        };
        var handler = new GetMiembrosHandler(repo);

        var result = await handler.Handle(new GetMiembrosQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        var miembro = Assert.Single(result);
        Assert.Equal(expected[0].UsuarioId, miembro.UsuarioId);
        Assert.Contains("Gluten", miembro.Alergias);
    }

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public bool IsUserInAnyHousehold { get; set; }
        public List<MiembroInfo> Miembros { get; set; } = [];

        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(IsUserInAnyHousehold);

        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(Miembros);

        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct) => Task.FromResult("token");
        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct) => Task.FromResult<InvitacionInfo?>(null);
        public Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(Guid.Empty);
        public Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(("user@test.com", "User"));
        public Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct) => Task.CompletedTask;
    }
}
