using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Hogares;

namespace Nido.Application.Tests.Hogares;

public sealed class CrearHogarHandlerTests
{
    [Fact]
    public async Task Handle_NombreValido_RetornaHogarYToken()
    {
        var repo    = new FakeHogarRepository();
        var invRepo = new FakeInvitacionRepository();
        var jwt     = new FakeJwt();
        var handler = new CrearHogarHandler(repo, invRepo, jwt);

        var result = await handler.Handle(
            new CrearHogarCommand(Guid.NewGuid(), "Casa de verano"),
            CancellationToken.None);

        Assert.Equal("Casa de verano", result.HogarNombre);
        Assert.NotEqual(Guid.Empty, result.HogarId);
        Assert.Equal("nuevo-jwt", result.NuevoToken);
    }

    [Fact]
    public async Task Handle_NombreVacio_LanzaArgumentException()
    {
        var handler = new CrearHogarHandler(new FakeHogarRepository(), new FakeInvitacionRepository(), new FakeJwt());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new CrearHogarCommand(Guid.NewGuid(), "   "), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NombreDemasiado_Largo_LanzaArgumentException()
    {
        var handler = new CrearHogarHandler(new FakeHogarRepository(), new FakeInvitacionRepository(), new FakeJwt());
        var nombreLargo = new string('x', 81);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new CrearHogarCommand(Guid.NewGuid(), nombreLargo), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NombreConEspaciosEnExtremos_SeGuardaTrimmeado()
    {
        var repo    = new FakeHogarRepository();
        var handler = new CrearHogarHandler(repo, new FakeInvitacionRepository(), new FakeJwt());

        var result = await handler.Handle(
            new CrearHogarCommand(Guid.NewGuid(), "  Casa  "),
            CancellationToken.None);

        Assert.Equal("Casa", result.HogarNombre);
    }

    private sealed class FakeHogarRepository : IHogarRepository
    {
        public Task<HogarInfo> CreateHogarAsync(Guid usuarioId, string nombre, CancellationToken ct)
            => Task.FromResult(new HogarInfo(Guid.NewGuid(), nombre));

        public Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct) => Task.FromResult<HogarInfo?>(null);
        public Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct) => Task.CompletedTask;
        public Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(new List<HogarConRolInfo>());
        public Task DeleteHogarAsync(Guid hogarId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeInvitacionRepository : IInvitacionRepository
    {
        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(("user@test.com", "Usuario Test"));

        public Task<bool> IsUserHouseholdOwnerAsync(Guid u, Guid h, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> IsMemberOfHouseholdAsync(Guid u, Guid h, CancellationToken ct) => Task.FromResult(false);
        public Task RemoveMiembroAsync(Guid h, Guid u, CancellationToken ct) => Task.CompletedTask;
        public Task<int> CountRealMembersAsync(Guid h, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid h, Guid i, string e, DateTime ex, CancellationToken ct) => Task.FromResult("");
        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string t, CancellationToken ct) => Task.FromResult<InvitacionInfo?>(null);
        public Task<bool> IsUserInAnyHouseholdAsync(Guid u, CancellationToken ct) => Task.FromResult(false);
        public Task AddUserToHouseholdAsync(Guid u, Guid h, string t, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid h, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
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
