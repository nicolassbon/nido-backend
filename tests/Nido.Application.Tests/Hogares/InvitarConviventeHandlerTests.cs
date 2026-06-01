using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class InvitarConviventeHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_RetornaTokenYEnviaEmail()
    {
        var repo = new FakeInvitacionRepository();
        var email = new FakeEmailService();
        var handler = new InvitarConviventeHandler(repo, email);

        var token = await handler.Handle(
            new InvitarConviventeCommand(repo.OwnerId, repo.HogarId, "invitado@mail.com"),
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal("invitado@mail.com", email.UltimoDestinatario);
    }

    [Fact]
    public async Task Handle_EmailVacio_LanzaMissingInvitationToken()
    {
        var repo = new FakeInvitacionRepository();
        var handler = new InvitarConviventeHandler(repo, new FakeEmailService());

        await Assert.ThrowsAsync<MissingInvitationTokenException>(() =>
            handler.Handle(
                new InvitarConviventeCommand(repo.OwnerId, repo.HogarId, "   "),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioNoEsOwner_LanzaNotHouseholdOwner()
    {
        var repo = new FakeInvitacionRepository { EsOwner = false };
        var handler = new InvitarConviventeHandler(repo, new FakeEmailService());

        await Assert.ThrowsAsync<NotHouseholdOwnerException>(() =>
            handler.Handle(
                new InvitarConviventeCommand(repo.OwnerId, repo.HogarId, "invitado@mail.com"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HogarLleno_LanzaMaxMembersExceeded()
    {
        var repo = new FakeInvitacionRepository { CantidadMiembros = 6 };
        var handler = new InvitarConviventeHandler(repo, new FakeEmailService());

        await Assert.ThrowsAsync<MaxMembersExceededException>(() =>
            handler.Handle(
                new InvitarConviventeCommand(repo.OwnerId, repo.HogarId, "invitado@mail.com"),
                CancellationToken.None));
    }

    // Test fakes

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public Guid OwnerId { get; } = Guid.NewGuid();
        public Guid HogarId { get; } = Guid.NewGuid();
        public bool EsOwner { get; set; } = true;
        public int CantidadMiembros { get; set; } = 2;

        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(EsOwner);

        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(CantidadMiembros);

        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct)
            => Task.FromResult("token-fake-123");

        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct)
            => Task.FromResult<InvitacionInfo?>(new InvitacionInfo(HogarId, "Casa de prueba", null, "pendiente", null));

        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(("owner@mail.com", "Dueño del hogar"));

        // Los métodos de abajo no se usan en InvitarConviventeHandler
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
        public Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
        public Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(false);
        public Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public string? UltimoDestinatario { get; private set; }

        public Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct)
        {
            UltimoDestinatario = toEmail;
            return Task.CompletedTask;
        }
    }
}
