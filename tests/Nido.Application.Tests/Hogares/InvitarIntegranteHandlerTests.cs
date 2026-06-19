using Nido.Application.Common.Notifications;
using Nido.Application.Tests.Common.Security;
using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class InvitarIntegranteHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_RetornaTokenYEnviaEmail()
    {
        var repo = new FakeInvitacionRepository();
        var membershipService = new RecordingHouseholdMembershipService();
        var email = new FakeEmailService();
        var handler = new InvitarIntegranteHandler(repo, membershipService, email);

        var token = await handler.Handle(
            new InvitarIntegranteCommand(repo.OwnerId, repo.HogarId, "invitado@mail.com"),
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal("invitado@mail.com", email.UltimoDestinatario);
        Assert.Single(membershipService.OwnerChecks);
    }

    [Fact]
    public async Task Handle_EmailVacio_LanzaMissingInvitationToken()
    {
        var repo = new FakeInvitacionRepository();
        var handler = new InvitarIntegranteHandler(repo, new RecordingHouseholdMembershipService(), new FakeEmailService());

        await Assert.ThrowsAsync<MissingInvitationTokenException>(() =>
            handler.Handle(
                new InvitarIntegranteCommand(repo.OwnerId, repo.HogarId, "   "),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioNoEsOwner_LanzaNotHouseholdOwner()
    {
        var membershipService = new RecordingHouseholdMembershipService
        {
            OwnerExceptionToThrow = new NotHouseholdOwnerException()
        };
        var handler = new InvitarIntegranteHandler(new FakeInvitacionRepository(), membershipService, new FakeEmailService());

        await Assert.ThrowsAsync<NotHouseholdOwnerException>(() =>
            handler.Handle(
                new InvitarIntegranteCommand(Guid.NewGuid(), Guid.NewGuid(), "invitado@mail.com"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HogarLleno_LanzaMaxMembersExceeded()
    {
        var repo = new FakeInvitacionRepository { CantidadMiembros = 6 };
        var handler = new InvitarIntegranteHandler(repo, new RecordingHouseholdMembershipService(), new FakeEmailService());

        await Assert.ThrowsAsync<MaxMembersExceededException>(() =>
            handler.Handle(
                new InvitarIntegranteCommand(repo.OwnerId, repo.HogarId, "invitado@mail.com"),
                CancellationToken.None));
    }

    // Test fakes

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public Guid OwnerId { get; } = Guid.NewGuid();
        public Guid HogarId { get; } = Guid.NewGuid();
        public int CantidadMiembros { get; set; } = 2;

        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(CantidadMiembros);

        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct)
            => Task.FromResult("token-fake-123");

        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct)
            => Task.FromResult<InvitacionInfo?>(new InvitacionInfo(HogarId, "Casa de prueba", null, "pendiente", null));

        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(("owner@mail.com", "Dueño del hogar"));

        // Los métodos de abajo no se usan en InvitarIntegranteHandler
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
        public Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
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

        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct) => Task.CompletedTask;

        public Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;

        public Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;
    }
}
