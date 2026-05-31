using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class AceptarInvitacionHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_RetornaHogarYNuevoToken()
    {
        var repo = new FakeInvitacionRepository();
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        var result = await handler.Handle(
            new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"),
            CancellationToken.None);

        Assert.Equal(repo.HogarDestino, result.HogarId);
        Assert.Equal("Casa destino", result.HogarNombre);
        Assert.Equal("nuevo-jwt", result.NuevoToken);
    }

    [Fact]
    public async Task Handle_TokenVacio_LanzaMissingInvitationToken()
    {
        var repo = new FakeInvitacionRepository();
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<MissingInvitationTokenException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "   "), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TokenNoExiste_LanzaInvitationNotFound()
    {
        var repo = new FakeInvitacionRepository { Invitacion = null };
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<InvitationNotFoundException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-invalido"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvitacionYaUsada_LanzaInvitationAlreadyProcessed()
    {
        var repo = new FakeInvitacionRepository
        {
            Invitacion = new InvitacionInfo(Guid.NewGuid(), "Casa destino", null, "aceptada", null)
        };
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<InvitationAlreadyProcessedException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvitacionExpirada_LanzaInvitationExpired()
    {
        var repo = new FakeInvitacionRepository
        {
            Invitacion = new InvitacionInfo(Guid.NewGuid(), "Casa destino", null, "pendiente", DateTime.UtcNow.AddDays(-1))
        };
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<InvitationExpiredException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HogarDestinoLleno_LanzaMaxMembersExceeded()
    {
        var repo = new FakeInvitacionRepository { CantidadMiembros = 6 };
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<MaxMembersExceededException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioYaEsMiembroDelHogar_LanzaAlreadyMember()
    {
        var repo = new FakeInvitacionRepository();
        // Hacemos que el hogar actual del usuario sea el mismo que el de la invitación
        repo.HogarActualDelUsuario = repo.HogarDestino;
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        await Assert.ThrowsAsync<AlreadyMemberException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioYaPerteneceAOtroHogar_LanzaNotSoleOwnerException()
    {
        var repo = new FakeInvitacionRepository { EsSoleOwner = false };
        var handler = new AceptarInvitacionHandler(repo, new FakeJwt());

        var ex = await Assert.ThrowsAsync<NotSoleOwnerException>(() =>
            handler.Handle(new AceptarInvitacionCommand(repo.UsuarioId, "token-valido"), CancellationToken.None));

        Assert.Equal("NOT_SOLE_OWNER", ex.Code);
    }

    // Test Fakes   

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public Guid UsuarioId { get; } = Guid.NewGuid();
        public Guid HogarDestino { get; } = Guid.NewGuid();
        public Guid HogarActualDelUsuario { get; set; } = Guid.NewGuid(); // distinto al destino por defecto
        public int CantidadMiembros { get; set; } = 2;
        public bool EsSoleOwner { get; set; } = true;

        public InvitacionInfo? Invitacion { get; set; }

        public FakeInvitacionRepository()
        {
            // Invitación válida por defecto
            Invitacion = new InvitacionInfo(HogarDestino, "Casa destino", null, "pendiente", null);
        }

        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct)
            => Task.FromResult(Invitacion);

        public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(CantidadMiembros);

        public Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(HogarActualDelUsuario);

        public Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(EsSoleOwner);

        public Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct)
            => Task.CompletedTask;

        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(("usuario@mail.com", "Nombre Usuario"));

        // Los métodos de abajo no se usan en AceptarInvitacionHandler
        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(true);
        public Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct) => Task.FromResult("");
        public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(false);
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "nuevo-jwt";
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("nuevo-jwt", "refresh", DateTime.UtcNow.AddDays(7));
    }
}
