using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class GetInvitacionPreviewHandlerTests
{
    [Fact]
    public async Task Handle_PendingInvitation_ReturnsPreview()
    {
        var repo = new FakeInvitacionRepository
        {
            Invitacion = new InvitacionInfo(Guid.NewGuid(), "Casa de prueba", "guest@test.com", "pendiente", DateTime.UtcNow.AddDays(2))
        };
        var handler = new GetInvitacionPreviewHandler(repo);

        var result = await handler.Handle("valid-token", CancellationToken.None);

        Assert.Equal("Casa de prueba", result.HogarNombre);
        Assert.Equal("guest@test.com", result.EmailInvitado);
        Assert.NotNull(result.ExpiraEn);
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ThrowsInvitationNotFound()
    {
        var repo = new FakeInvitacionRepository { Invitacion = null };
        var handler = new GetInvitacionPreviewHandler(repo);

        await Assert.ThrowsAsync<InvitationNotFoundException>(() =>
            handler.Handle("missing-token", CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvitationAlreadyProcessed_ThrowsInvitationAlreadyProcessed()
    {
        var repo = new FakeInvitacionRepository
        {
            Invitacion = new InvitacionInfo(Guid.NewGuid(), "Casa de prueba", "guest@test.com", "aceptada", DateTime.UtcNow.AddDays(2))
        };
        var handler = new GetInvitacionPreviewHandler(repo);

        await Assert.ThrowsAsync<InvitationAlreadyProcessedException>(() =>
            handler.Handle("processed-token", CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredInvitation_ThrowsInvitationExpired()
    {
        var repo = new FakeInvitacionRepository
        {
            Invitacion = new InvitacionInfo(Guid.NewGuid(), "Casa de prueba", "guest@test.com", "pendiente", DateTime.UtcNow.AddMinutes(-1))
        };
        var handler = new GetInvitacionPreviewHandler(repo);

        await Assert.ThrowsAsync<InvitationExpiredException>(() =>
            handler.Handle("expired-token", CancellationToken.None));
    }

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public InvitacionInfo? Invitacion { get; set; }

        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct)
            => Task.FromResult(Invitacion);

        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct) => Task.FromResult("token");
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task AddUserToHouseholdAsync(Guid usuarioId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(("user@test.com", "User"));
        public Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct) => Task.CompletedTask;
    }
}
